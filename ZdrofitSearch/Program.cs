using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Net;
using System.Net.Http.Headers;
using ZdrofitSearch;

HttpClient httpClient = new HttpClient();

ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

httpClient.BaseAddress = new Uri("https://foobar.com/");
httpClient.DefaultRequestHeaders.Accept.Clear();
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

const string BaseUrl = "https://zdrofit.pl/";

var clubs = new Dictionary<string, string>
{
    {"Grójecka", "https://zdrofit.pl/kluby-fitness/warszawa-ochota-grojecka/grafik-zajec" },
    {"Jerozolimskie", "https://zdrofit.pl/kluby-fitness/warszawa-ochota-jerozolimskie/grafik-zajec" },
    {"Bukowińska", "https://zdrofit.pl/kluby-fitness/warszawa-mokotow-metro/grafik-zajec" },
    {"Żwirki i Wigury", "https://zdrofit.pl/kluby-fitness/warszawa-wlochy-zwirki-i-wigury/grafik-zajec" }
};

var monthNames = new[] {"stycznia", "lutego", "marca", "kwietnia",
    "maja", "czerwca", "lipca", "sierpnia", "września",
    "pażdziernika", "listopada", "grudnia" };

var relevantTrainings = new[] { "sztangi", "zdrowy kręgosłup" };

List<ScheduleItem> relevantItems = new List<ScheduleItem>();

IHtmlDocument doc;

foreach (var club in clubs)
{
    using (var stream = await httpClient.GetStreamAsync(new Uri(club.Value)))
    {
        var parser = new HtmlParser();
        doc = await parser.ParseDocumentAsync(stream);
    }

    var scheduleTable = doc.QuerySelector("section[class='js-filters club-schedule']>table");

    var tHeaders = scheduleTable.QuerySelector("thead").QuerySelectorAll("th");

    var days = tHeaders.Select(x => x.QuerySelector("strong")?.InnerHtml).Where(y => y != null).ToArray();

    var tRows = scheduleTable.QuerySelector("tbody").QuerySelectorAll("tr");

    var itemList = new List<ScheduleItem>();

    foreach (var row in tRows)
    {
        var th = row.QuerySelector("th").InnerHtml;

        var tds = row.QuerySelectorAll("td");

        for (int j = 0; j < tds.Length; j++)
        {
            var td = tds[j];
            var divs = td.QuerySelectorAll("div[class=club-schedule-item]");

            if (divs != null)
            {
                foreach (var div in divs)
                {
                    var strongElems = div.QuerySelectorAll("strong");

                    if (strongElems != null)
                    {
                        var trainingLinkElem = div.QuerySelector("a");
                        var registrationLinkElem = div.QuerySelector("a[class=register]");

                        var name = trainingLinkElem != null ? trainingLinkElem.InnerHtml
                            : strongElems[0].QuerySelector("span").InnerHtml;

                        var registrationLink = registrationLinkElem != null ? registrationLinkElem.Attributes["href"].Value : null;

                        var time = strongElems[1].InnerHtml.Trim('\t');

                        itemList.Add(new ScheduleItem
                        {
                            ClubName = club.Key,
                            RegistrationLink = registrationLink,
                            Date = days[j],
                            Name = name,
                            Time = time
                        });
                    }
                }
            }
        }
    }

    

    relevantItems.AddRange(itemList.Where(x => relevantTrainings.Contains(x.Name.ToLower())).ToList());
    
}

var monthItemList = new List<MonthItems>();

foreach (var monthName in monthNames)
{
    var monthItems = relevantItems.Where(x => x.Date.ToLower().Contains(monthName));

    monthItemList.Add(new MonthItems
    {
        MonthName = monthName,
        DateItems = monthItems.GroupBy(y => y.Date).Select(
            x => new DateItems
            {
                Date = x.Key,
                Items = x.Select(z => z).ToList()
            }).ToList()
    });
}

foreach (var monthItem in monthItemList)
{
    foreach (var dateItem in monthItem.DateItems.OrderBy(x => x.Date))
    {
        Console.WriteLine(dateItem.Date);
        foreach (var item in dateItem.Items.OrderBy(x => x.Time))
        {
            var registrationLink = item.RegistrationLink != null ? BaseUrl + item.RegistrationLink.TrimStart('/') : null;
            Console.WriteLine($"{item.Time} - {item.Name} - {item.ClubName} - {registrationLink}");
        }
        Console.WriteLine("----------");
    }
}

