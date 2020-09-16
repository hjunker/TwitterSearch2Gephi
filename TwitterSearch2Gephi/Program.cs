using System;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using Tweetinvi.Parameters;
using Tweetinvi.Models.DTO;
using Tweetinvi.Logic.JsonConverters;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using HtmlAgilityPack;
using System.Net;

namespace TwitterSearch2Gephi
{
    class Program
    {
        public static int counter = 0;

        public static void writeLine(Char dataset, DateTime dt, String nameA, String nameB, String kind, String targetfile, int weight)
        {
            String res = "";
            String timeset = dt.ToUniversalTime().ToString("u");
            timeset = "<[" + timeset.Replace(' ', 'T') + "]>"; 
            if (dataset.Equals('t'))
            {
                res = "@" + nameA + ",@" + nameB + ",Directed," + kind + "," + counter + ",," + timeset + "," + weight;
            }
            if (dataset.Equals('w'))
            {
                res = nameA + "," + nameB + ",Directed," + kind + "," + counter + ",," + timeset + "," + weight;
            }
            File.AppendAllText(targetfile, res + "\n");
            counter++;
        }

        public static async void sendToGephi(DateTime dt, String nameA, String nameB, String kind, int weight)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "http://localhost:8080/a1?operation=updateGraph");
            //"http://localhost:8080/a1?operation=updateGraph" -d "{\"ae\":{\"AB\":{\"source\":\"A\",\"target\":\"B\",\"directed\":false}}}
            //String jsonpayload = "{\"ae\":{\"AB\":{\"source\":\"A\",\"target\":\"B\",\"directed\":false}}}";
            String jsonpayload = "{\"an\":{\"" + nameA + "\":{\"label\":\"" + nameA + "\"}}}";
            var stringcontent = new StringContent(jsonpayload, Encoding.UTF8, "application/json");
            request.Content = stringcontent;
            var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "http://localhost:8080/a1?operation=updateGraph");
            jsonpayload = "{\"an\":{\"" + nameB + "\":{\"label\":\"" + nameB + "\"}}}";
            stringcontent = new StringContent(jsonpayload, Encoding.UTF8, "application/json");
            request.Content = stringcontent;
            response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            // TODO: update existing edges (weighted!)
            request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "http://localhost:8080/a1?operation=updateGraph");
            jsonpayload = "{\"ae\":{\"" + nameB + "-" + nameA + "\":{\"source\":\"" + nameB + "\",\"target\":\"" + nameA + "\",\"directed\":false}}}";
            stringcontent = new StringContent(jsonpayload, Encoding.UTF8, "application/json");
            request.Content = stringcontent;
            response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
        }

        public static void handleUser1(String handle, String targetfile, int depth, int maxdepth, int output)
        {
            //int maxdepth = 1;
            try
            {
                IUser user = null;

                user = User.GetUserFromScreenName(handle);
                if (user == null) Console.WriteLine("ERROR GETTING USER");
                Console.WriteLine(user.ScreenName + " / " + user.Name + " / " + user.IdStr);
                
                try
                {
                    System.Collections.Generic.IEnumerable<IUser> enumfollowers = user.GetFollowers(2500);
                    Console.WriteLine("followers found: " + enumfollowers.Count());
                    if (enumfollowers != null)
                    {
                        foreach (IUser tmpuser in enumfollowers)
                        {
                            DateTime timeset = tmpuser.CreatedAt;

                            if (output == 1)
                            {
                                writeLine('t', timeset, user.ScreenName, tmpuser.ScreenName, "FollowedBy", targetfile, 2);
                            }
                            else
                            {
                                sendToGephi(timeset, user.ScreenName, tmpuser.ScreenName, "FollowedBy", 2);
                            }

                            if (depth < maxdepth) handleUser1(tmpuser.ScreenName, targetfile, depth + 1, maxdepth, output);
                        }
                    }
                    else
                    {
                        Console.WriteLine("no followers found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
                
                try
                {
                    System.Collections.Generic.IEnumerable<IUser> enumfriends = user.GetFriends(1000);
                    if (enumfriends != null)
                    {
                        foreach (IUser tmpuser in enumfriends)
                        {
                            DateTime timeset = tmpuser.CreatedAt;

                            if (output == 1)
                            {
                                writeLine('t', timeset, tmpuser.ScreenName, user.ScreenName, "FriendOf", targetfile, 5);
                            }
                            else
                            {
                                sendToGephi(timeset, user.ScreenName, tmpuser.ScreenName, "FollowedBy", 5);
                            }

                            if (depth < maxdepth) handleUser1(tmpuser.ScreenName, targetfile, depth + 1, maxdepth, output);
                        }
                    }
                }
                catch (Exception)
                { }
                
                try
                {
                    System.Collections.Generic.IEnumerable<ITweet> favtweets = user.GetFavorites(200);
                    if (favtweets != null)
                    {
                        foreach (ITweet favoredtweet in favtweets)
                        {
                            DateTime timeset = favoredtweet.CreatedAt;

                            if (output == 1)
                            {
                                writeLine('t', timeset, favoredtweet.CreatedBy.ScreenName, user.ScreenName, "FavoredBy", targetfile, 1);
                            }
                            else
                            {
                                sendToGephi(timeset, favoredtweet.CreatedBy.ScreenName, user.ScreenName, "FavoredBy", 5);
                            }

                            //if (depth < maxdepth) handleUser1(favoredtweet.CreatedBy.ScreenName, targetfile, depth + 1);
                        }
                    }
                }
                catch (Exception)
                { }

                try
                {
                    if (user.Retweets != null)
                    {
                        foreach (ITweet retweet in user.Retweets)
                        {
                            DateTime timeset = retweet.CreatedAt;

                            if (output == 1)
                            {
                                writeLine('t', timeset, retweet.CreatedBy.ScreenName, user.ScreenName, "RetweetedBy", targetfile, 2);
                            }
                            else
                            {
                                sendToGephi(timeset, retweet.CreatedBy.ScreenName, user.ScreenName, "RetweetedBy", 5);
                            }

                            //if (depth < maxdepth) handleUser1(favoredtweet.CreatedBy.ScreenName, targetfile, depth + 1);
                        }
                    }
                }
                catch (Exception)
                { }

                //counter++;
                System.Threading.Thread.Sleep(1000 * 60);


                //user.TweetsRetweetedByFollowers;
            }
            catch (Exception)
            {
                Console.WriteLine("error during processing of user " + handle.ToString());
            }
        }

        public static void searchTerm(String term, String targetfile, int depth, int maxdepth, int output)
        {
            //int maxdepth = 1;
            try
            {
                var searchParameter = new SearchTweetsParameters("TwitterSearch2Gephi")
                {
                    //GeoCode = new GeoCode(-122.398720, 37.781157, 1, DistanceMeasure.Miles),
                    //Lang = LanguageFilter.English,
                    //SearchType = SearchResultType.Popular,
                    //MaximumNumberOfResults = 100,
                    //Until = new DateTime(2015, 06, 02),
                    //SinceId = 399616835892781056,
                    //MaxId = 405001488843284480,
                    //Filters = TweetSearchFilters.Images | TweetSearchFilters.Verified
                };
                var tweets = Tweetinvi.Search.SearchTweets(searchParameter).ToList();
                var tweetList = new List<ITweet>(tweets);


                //IUser user = null;

                foreach (ITweet tweet in tweetList)
                {
                    handleUser1(tweet.CreatedBy.ScreenName, targetfile, depth, maxdepth, output);
                }

            }
            catch (Exception)
            {
                Console.WriteLine("error...");
            }
        }

        public static void crawlDomain(String dom, int cnt, Dictionary<string, int> wwwurlstats, bool recursive)
        {
            Uri url;
            String[] socialNetworks = { "vk.com", "youtube.com", "twitter.com", "facebook.com", "patreon.com", "bitchute.com" };
            int len;
            String test;

            WebClient client = new WebClient();

            //Console.WriteLine("DOMAIN: " + dom);
            try
            {
                test = client.DownloadString(dom);
                //Console.WriteLine("HTML length: " + test.Length);
                Uri testdom = new Uri(dom);

                HtmlDocument document = new HtmlDocument();
                //document.Load(@"C:\Temp\sample.txt");
                document.LoadHtml(test);


                //Console.WriteLine("TITLE: " + document.DocumentNode.SelectSingleNode("//head/title").InnerText);
                IEnumerable<HtmlNode> links = document.DocumentNode.Descendants("a");
                foreach (HtmlNode node in links)
                {
                    //Console.WriteLine(dom + "," + document.DocumentNode.SelectSingleNode("//head/title").InnerText + "," + node.GetAttributeValue("href", null) + "," + node.InnerText.Replace("\n", ""));
                    File.AppendAllText(@"C:\TwitterSearch2Gephi\edges.csv", dom + "," + document.DocumentNode.SelectSingleNode("//head/title").InnerText + "," + node.GetAttributeValue("href", null) + "," + node.InnerText.Replace("\n", "").Replace(",", ";") + "\n");
                    //if ((recursive == true) & (!node.GetAttributeValue("href", null).Contains(dom))) crawlDomain(node.GetAttributeValue("href", null), cnt, wwwurlstats, false);
                }

                IEnumerable<HtmlNode> linkTags = document.DocumentNode.Descendants("link");
                IEnumerable<String> linkedPages = document.DocumentNode.Descendants("a")
                                                  .Select(a => a.GetAttributeValue("href", null))
                                                  .Where(u => !String.IsNullOrEmpty(u));
                foreach (String lp in linkedPages)
                {
                    //Console.WriteLine(lp);
                    try
                    {
                        url = new Uri(lp);
                        cnt++;

                        if (socialNetworks.Any(lp.Contains))
                        {
                            //Console.WriteLine(lp);
                        }

                        //Source,Target,Type,Kind,Id,Label,timeset,Weight
                        String sourcestr = testdom.Host;
                        if (sourcestr.Contains("www."))
                        {
                            sourcestr = sourcestr.Substring(sourcestr.IndexOf("www.") + 4);
                        }
                        String targetstr = url.Host;
                        if (targetstr.Contains("www."))
                        {
                            targetstr = targetstr.Substring(targetstr.IndexOf("www.") + 4);
                        }
                        if (sourcestr.Contains(targetstr))
                        {
                            sourcestr = targetstr;
                        }
                        else if (targetstr.Contains(sourcestr))
                        {
                            targetstr = sourcestr;
                        }
                        String gephiline = sourcestr + "," + targetstr + ",Directed,linkto," + cnt + ",,,1";
                        //Console.WriteLine(gephiline);
                        File.AppendAllText(@"C:\TwitterSearch2Gephi\edges.csv", gephiline + "\n");

                        if (wwwurlstats.ContainsKey(url.Host))
                        {
                            int tmpval;
                            wwwurlstats.TryGetValue(url.Host, out tmpval);
                            wwwurlstats.Remove(url.Host);
                            wwwurlstats.Add(url.Host, tmpval + 1);
                        }
                        else
                        {
                            wwwurlstats.Add(url.Host, 1);
                        }
                    }
                    catch (Exception)
                    { }
                }
            }
            catch (Exception)
            {
                //Console.WriteLine("!!! ERROR HANDLING DOMAIN " + dom);
            }
        }

        static void Main(string[] args)
        //static async Task Main(string[] args)
        {
            String outdir = @"C:\TwitterSearch2Gephi";
            String outfile = outdir + @"\edges.csv";
            String ewfile = outdir + @"\edges-weighted.csv";
            String accountsfile = outdir + @"\accounts.txt";
            String domainsfile = outdir + @"\domains.txt";
            String searchtermsfile = outdir + @"\searchterms.txt";
            String credentialsfile = outdir + @"\credentials.txt";
            String userchoice = "";
            ConsoleKeyInfo choice;

            Console.WriteLine("TwitterSearch2Gephi - @DisinfoG");
            Console.WriteLine("https://twitter.com/DisinfoG \n");
            Console.WriteLine("https://github.com/hjunker/TwitterSearch2Gephi");
            Console.WriteLine("using folder " + outdir);
            Console.WriteLine("");
            Console.Write("What should I do for you?");
            Console.WriteLine("\ntwitter\n-------");
            Console.WriteLine("a - crawl twitter accounts from accounts.txt and create edges.csv");
            Console.WriteLine("s - collect twitter data from search terms in searchterms.txt and create edges.csv");
            Console.WriteLine("\nwww\n---");
            Console.WriteLine("d - crawl web domains / URLs from domains.txt and create edges.csv");
            Console.WriteLine("\nreddit\n------");
            Console.WriteLine("r - crawl reddit accounts from redditaccounts.txt and create edges.csv");
            Console.WriteLine("\ngeneral\n-------");
            Console.WriteLine("w - create weighted edges file (from edges.csv to edges-weighted.csv)");
            Console.WriteLine("c - clique-analysis from edges-weighted.csv (early stage PoC)");
            Console.Write("\nWhat should I do for you?");
            choice = Console.ReadKey();
            Console.WriteLine("\n");
            userchoice += choice.KeyChar;

            if (choice.KeyChar == 'c')
            {
                String nodeA, nodeB;
                // patient zero account(s) P0
                String p0 = "Zeitgeschehen_";


                // get all nodes / accounts from edges-weighted.csv
                String[] nodestmp = File.ReadAllLines(ewfile);
                for (int i=0; i<nodestmp.Length; i++)
                {
                    nodestmp[i] = nodestmp[i].Substring(1, nodestmp[i].IndexOf(',') - 1);
                }
                List<String> nodes = new List<String>();
                foreach (String tmp in nodestmp)
                {
                    if (!nodes.Contains(tmp))
                    {
                        nodes.Add(tmp);
                    }
                }

                // get all edges / engagements
                String[] edges = File.ReadAllLines(ewfile);


                // get engagers of P0
                List<String> p0engagers = new List<String>();
                foreach (String edge in edges)
                {
                    String[] csvvalues = edge.Split(',');
                    nodeA = csvvalues[0].Substring(1);
                    nodeB = csvvalues[1].Substring(1);
                    if ((nodeA.Equals(p0)) & (!p0engagers.Contains(nodeB)))
                    {
                        p0engagers.Add(nodeB);
                    }
                    if ((nodeB.Equals(p0)) & (!p0engagers.Contains(nodeA)))
                    {
                        p0engagers.Add(nodeA);
                    }
                }
                Console.WriteLine("engagers of p0 " + p0 + "(" + p0engagers.Count  + ")");
                foreach (String tmp in p0engagers)
                {
                    Console.WriteLine(tmp);
                }
                Console.WriteLine("\n\n");

                // for each node check matching engagers in comparison to P0'
                int counter = 0;
                String[] edgevalues;
                
                foreach (String node in nodes)
                {
                    counter = 0;
                    foreach (String edge in edges)
                    {
                        edgevalues = edge.Split(',');
                        foreach (String eng in p0engagers)
                        {
                            nodeA = edgevalues[0].Substring(1);
                            nodeB = edgevalues[1].Substring(1);
                            //if (((nodeA.Equals(eng)) & (nodeB.Equals(node))) | ((nodeB.Equals(eng)) & (nodeA.Equals(node))))
                            if ((edge.Contains(eng)) & (edge.Contains(node)))
                            {
                                counter++;
                                //Console.WriteLine(eng + " in " + edge + "\n");
                                break;
                            }
                        }
                    }
                    Console.WriteLine(node + ": " + counter + " / " + p0engagers.Count);
                }


            }

                if (choice.KeyChar == 's')
            {
                int maxdepth = 1;
                int output = 1;

                Console.WriteLine("this functionality is not completed yet...");

                try
                {
                    Console.Write("maxdepth for recursion (default=1): ");
                    maxdepth = int.Parse(Console.ReadLine());
                    Console.WriteLine("");
                }
                catch (Exception)
                {
                    Console.WriteLine("no maxdepth given as first parameter. setting maxdepth = 1\n");
                }

                try
                {
                    Console.Write("output to file (1) or gephi web interface (2) (default=1): ");
                    output = int.Parse(Console.ReadLine());
                    Console.WriteLine("");
                }
                catch (Exception)
                {
                    Console.WriteLine("error reading your input.\n");
                }

                // Create Directory
                System.IO.Directory.CreateDirectory(outdir);

                String[] searchterms = null;
                try
                {
                    searchterms = File.ReadAllLines(searchtermsfile);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error reading " + searchtermsfile);
                    Console.ReadKey();
                }

                String[] credentials = null;
                try
                {
                    credentials = File.ReadAllLines(credentialsfile);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error reading " + credentialsfile + ". Please remember the file needs to contain the 4 credential parameters for your twitter developer account / app one per line in the order consumerKey, consumerSecret, userAccessToken, userAccessSecret");
                    Console.ReadKey();
                }

                ITwitterCredentials creds;
                creds = Auth.SetUserCredentials(credentials[0], credentials[1], credentials[2], credentials[3]);

                File.WriteAllText(outfile, "Source,Target,Type,Kind,Id,Label,timeset,Weight\n");


                // MANUAL MAPPING OF ACCOUNTS, e.g. RTDeutsch and russiatoday, also for cross social-media mapping, ...
                /*
                DateTime timeset = DateTime.Now;
                writeLine(timeset, "HolgerJunker", "DisinfoG", "RetweetedBy", outfile, 50);
                */

                Console.WriteLine("Starting to collect engagements data from accounts...");

                foreach (String term in searchterms)
                {
                    searchTerm(term, outfile, 0, maxdepth, output);
                }
            }

            if (choice.KeyChar == 'a')
            {
                int maxdepth = 1;
                int output = 1;

                try
                {
                    Console.Write("maxdepth for recursion (default=1): ");
                    maxdepth = int.Parse(Console.ReadLine());
                    Console.WriteLine("");
                }
                catch (Exception)
                {
                    Console.WriteLine("no maxdepth given as first parameter. setting maxdepth = 1\n");
                }

                try
                {
                    Console.Write("output to file (1) or gephi web interface (2) (default=1): ");
                    output = int.Parse(Console.ReadLine());
                    Console.WriteLine("");
                }
                catch (Exception)
                {
                    Console.WriteLine("error reading your input.\n");
                }

                // Create Directory
                System.IO.Directory.CreateDirectory(outdir);

                String[] accounts = null;
                try
                {
                    accounts = File.ReadAllLines(accountsfile);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error reading " + accountsfile);
                    Console.ReadKey();
                }

                String[] credentials = null;
                try
                {
                    credentials = File.ReadAllLines(credentialsfile);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error reading " + credentialsfile + ". Please remember the file needs to contain the 4 credential parameters for your twitter developer account / app one per line in the order consumerKey, consumerSecret, userAccessToken, userAccessSecret");
                    Console.ReadKey();
                }

                ITwitterCredentials creds;
                creds = Auth.SetUserCredentials(credentials[0], credentials[1], credentials[2], credentials[3]);

                File.WriteAllText(outfile, "Source,Target,Type,Kind,Id,Label,timeset,Weight\n");


                // MANUAL MAPPING OF ACCOUNTS, e.g. RTDeutsch and russiatoday, also for cross social-media mapping, ...
                /*
                DateTime timeset = DateTime.Now;
                writeLine(timeset, "HolgerJunker", "DisinfoG", "RetweetedBy", outfile, 50);
                */

                Console.WriteLine("Starting to collect engagements data from accounts...");

                foreach (String handle in accounts)
                {
                    handleUser1(handle, outfile, 0, maxdepth, output);
                }
            }

            // --------------------------------------------------------------

            if (choice.KeyChar == 'w')
            {
                Console.WriteLine("converting edges.csv to edges-weighted.csv");

                File.WriteAllText(outdir + @"\edges-weighted.csv", "Source,Target,Type,Kind,Id,Label,timeset,Weight\n");

                Dictionary<string, int> edges = new Dictionary<string, int>();
                Dictionary<string, int> spreaders = new Dictionary<string, int>();

                String[] lines = File.ReadAllLines(outfile);

                Char dataset = '-';
                if (lines[0].Equals("Source,Target,Type,Kind,Id,Label,timeset,Weight")) dataset = 't';
                if (lines[0].Equals("domain,title,link,linktext")) dataset = 'w';

                    for (int i=1; i<lines.Length; i++)
                {
                    // values: Source,Target,Type,Kind,Id,Label,timeset,Weight
                    // Kind: RetweetedBy / FollowedBy / FriendOf / 
                    String[] values = lines[i].Split(',');

                    // combine Source and Target to Key: Source,Target
                    String key = "";
                    if (String.Compare(values[0], values[1], StringComparison.InvariantCulture) > 0)
                    {
                        key = values[0] + "," + values[1];
                    }
                    else
                    {
                        key = values[1] + "," + values[0];
                    }

                    int weight = 1;
                    if (values[3] == "RetweetedBy")
                    {
                        weight = 3;
                    }
                    if (values[3] == "FollowedBy")
                    {
                        weight = 1;
                    }
                    if (values[3] == "FriendOf")
                    {
                        weight = 5;
                    }

                    // insert / update edge entry in Dictionary edges
                    if (edges.ContainsKey(key))
                    {
                        int oldval;
                        edges.TryGetValue(key, out oldval);
                        edges.Remove(key);
                        edges.Add(key, oldval + weight);
                    }
                    else
                    {
                        edges.Add(key, weight);
                    }

                    if (spreaders.ContainsKey(values[1]))
                    {
                        int oldval;
                        spreaders.TryGetValue(values[1], out oldval);
                        spreaders.Remove(values[1]);
                        spreaders.Add(values[1], oldval + weight);
                    }
                    else
                    {
                        spreaders.Add(values[1], weight);
                    }
                }

                Console.WriteLine("SPREADERS\n---------");
                foreach (KeyValuePair<string, int> tmp in spreaders)
                {
                    //File.AppendAllText(outdir + @"\edges-weighted.csv", tmp.Key + "," + tmp.Value + "\n");
                    Console.WriteLine(tmp.Key + ", " + tmp.Value);
                }
                Console.WriteLine("\n\n");

                // write Dictionary edges to edges-weighted.csv
                Console.WriteLine("\n\n Writing edges-weighted.csv");
                // dummy for timeset
                DateTime timesetdummy = DateTime.Now;

                foreach (KeyValuePair<string, int> tmp in edges)
                {
                    //File.AppendAllText(outdir + @"\edges-weighted.csv", tmp.Key + "," + tmp.Value + "\n");
                    String[] accounts = tmp.Key.Split(',');
                    try
                    {
                        // FOR TWITTER:
                        if (dataset.Equals('t'))
                        {
                            writeLine(dataset, timesetdummy, accounts[0].Substring(1), accounts[1].Substring(1), "aggregated", outdir + @"\edges-weighted.csv", tmp.Value);
                        }

                        // FOR WWW:
                        if (dataset.Equals('w'))
                        {
                            writeLine(dataset, timesetdummy, accounts[0], accounts[1], "aggregated", outdir + @"\edges-weighted.csv", tmp.Value);
                        }
                        
                    }
                    catch (Exception)
                    { }
                }
            }

            if (choice.KeyChar == 'd')
            {
                String[] domains = File.ReadAllLines(domainsfile);
                int cnt = 0;
                Dictionary<string, int> wwwurlstats = new Dictionary<string, int>();

                //File.WriteAllText(@"C:\TwitterSearch2Gephi\edges.csv", "Source,Target,Type,Kind,Id,Label,timeset,Weight\n");
                File.WriteAllText(@"C:\TwitterSearch2Gephi\edges.csv", "domain,title,link,linktext\n");

                //IEnumerable<String> ALLdomains = DEdomains.Concat(RUSdomains);

                foreach (String dom in domains)
                {
                    crawlDomain(dom, cnt, wwwurlstats, true);
                }

                Console.WriteLine("\nStatistik:\n");

                foreach (var stat in wwwurlstats)
                {
                    Console.WriteLine(stat.Key + "," + stat.Value);
                }
            }

                // --------------------------------------------------------------

                Console.WriteLine("\nFINISHED. Hit key to exit.");
            Console.WriteLine("\nIf you like my work, feel free to support my open source projects via paypal (https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=WLC2SHZL6SPNY) or amazon wishlist (https://www.amazon.de/hz/wishlist/ls/2FD1Z75K43I7M?ref_=wl_share).");
            Console.ReadKey();
        }
    }
}
