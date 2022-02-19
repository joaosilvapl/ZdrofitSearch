using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Net;
using System.Net.Http.Headers;
using ZdrofitSearch;

HttpClient httpClient = new HttpClient();

System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

httpClient.BaseAddress = new Uri("https://foobar.com/");
httpClient.DefaultRequestHeaders.Accept.Clear();
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

var url = "https://zdrofit.pl/kluby-fitness/warszawa-ochota-grojecka/grafik-zajec";

//var response = await httpClient.GetAsync(url);

//var msg = await response.Content.ReadAsStringAsync();

IHtmlDocument doc;

using (var stream = await httpClient.GetStreamAsync(new Uri(url)))
{

    //to parse the HTML to AngleSharp.Parser.Html.HtmlParser object 

    var parser = new HtmlParser();
    doc = await parser.ParseDocumentAsync(stream);
}

//var elems = doc.QuerySelectorAll("div[class=club-schedule-item]");
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

                    var linkElem = div.QuerySelector("a");

                    var name = linkElem != null ? linkElem.InnerHtml
                        : strongElems[0].QuerySelector("span").InnerHtml;

                    var time = strongElems[1].InnerHtml.Trim('\t');

                    itemList.Add(new ScheduleItem
                    {
                        Date = days[j],
                        Name = name,
                        Time = time
                    });
                }
            }
        }
    }
}

var relevantNames = new[] { "sztangi", "zdrowy kręgosłup" };

var relevantItems = itemList.Where(x => relevantNames.Contains(x.Name.ToLower()));

//var groupedItems = relevantItems.GroupBy(x => x.Date);

var monthNames = new[] {"stycznia", "lutego", "marca", "kwietnia",
    "maja", "czerwca", "lipca", "sierpnia", "września",
    "pażdziernika", "listopada", "grudnia" };

var monthItemList = new List<MonthItems>();

foreach (var monthName in monthNames)
{
    var monthItems = relevantItems.Where(x => x.Date.ToLower().Contains(monthName));

    monthItemList.Add(new MonthItems
    {
        MonthName = monthName,
        DateItems = monthItems.GroupBy(y => y.Date).Select(
            x => new DateItems { Date = x.Key,
            Items = x.Select(z => z).ToList()}).ToList()
    });
}

foreach (var monthItem in monthItemList)
{
    foreach (var dateItem in monthItem.DateItems.OrderBy(x => x.Date))
    {
        Console.WriteLine(dateItem.Date);
        foreach (var item in dateItem.Items.OrderBy(x => x.Time))
        {
            Console.WriteLine($"{item.Time} - {item.Name}");
        }
        Console.WriteLine("----------");
    }
}

Console.ReadKey();
