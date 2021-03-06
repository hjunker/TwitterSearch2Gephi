﻿using System;
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
//using Reddit;
//using Reddit.Controllers;
using User = Tweetinvi.User;
using Newtonsoft.Json.Linq;

namespace TwitterSearch2Gephi
{
    class MyWebClient : WebClient
    {
        Uri _responseUri;

        public Uri ResponseUri
        {
            get { return _responseUri; }
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            try
            {
                WebResponse response = base.GetWebResponse(request);
                _responseUri = response.ResponseUri;
                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    class Program
    {
        public static int counter = 0;
        public static Dictionary<string, int> processedAccounts = new Dictionary<string, int>();

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

        public static void writeLineUci(String nameA, String nameB, String kind, String targetfile)
        {
            String res = nameA + "," + nameB + "," + kind;
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

        public static String stripString(String instr)
        {
            String res = "";
            Encoding ascii = Encoding.ASCII;
            Byte[] encodedBytes = ascii.GetBytes(instr);
            String decodedString = ascii.GetString(encodedBytes);
            res = decodedString.Replace('\\', ' ').Replace(',', ';').Replace('\n', '/').Replace('"', ' ').Replace('\'', ' ');
            return res;
        }

        public static void getTwitterNode(IUser user)
        {
            /*
            String res = "@" + user.ScreenName + ",";
            res += user.ScreenName + ",";
            res += user.FriendsCount + ",";
            res += user.FollowersCount + ",";
            */

            // TODO: string values in "..." after doing some good casting/encoding/...
            
            //Id,Label,timeset,twitter_type,lat,lng,place_country,place_type,place_fullname,place_name,
            //created_at,lang,possibly_sensitive,quoted_status_permalink,description,email,profile_image,
            //friends_count,followers_count,real_name,location,emoji_alias,emoji_html_decimal,emoji_utf8
            //String res = user.IdStr + ",";
            String res = "@" + user.ScreenName + ",";
            res += user.ScreenName + ",";
            res += user.CreatedAt.ToString("s") + "Z" + ",";
            res += "User" + ",";
            res += ",";
            res += ",";
            res += ",";
            res += ",";
            res += ",";
            res += ",";
            res += user.CreatedAt.ToString() + ",";
            res += ",";
            res += ",";
            res += ",";
            res += "\"" + stripString(user.Description) + "\",";
            res += ",";
            res += user.ProfileImageUrl + ",";
            res += user.FriendsCount + ",";
            res += user.FollowersCount + ",";
            res += "\"" + stripString(user.Name) + "\",";
            res += "\"" + stripString(user.Location) + "\",";
            res += ",";
            res += ",";
            res += "";
            
            File.AppendAllText(@"C:\TwitterSearch2Gephi\nodes.csv", res + "\n");
        }

        public static void handleUser1(String term, String handle, String targetfile, int depth, int maxdepth, int output)
        {
            //int maxdepth = 1;
            try
            {
                IUser user = null;

                user = User.GetUserFromScreenName(handle);
                if (user == null) Console.WriteLine("ERROR GETTING USER");
                Console.WriteLine(user.ScreenName + " / " + user.Name + " / " + user.IdStr);
                getTwitterNode(user);

                if (term != null)
                {
                    DateTime timeset = DateTime.Now;
                    if (output == 1)
                    {
                        writeLine('t', timeset, user.ScreenName, term, "LinkTo", targetfile, 10);
                    }
                    else
                    {
                        sendToGephi(timeset, user.ScreenName, term, "LinkTo", 10);
                    }
                }
                
                try
                {
                    System.Collections.Generic.IEnumerable<IUser> enumfollowers = user.GetFollowers(250);
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
                            getTwitterNode(tmpuser);

                            if (depth < maxdepth) handleUser1(null, tmpuser.ScreenName, targetfile, depth + 1, maxdepth, output);
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
                
                /*
                try
                {
                    System.Collections.Generic.IEnumerable<IUser> enumfriends = user.GetFriends(50);
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
                            getTwitterNode(tmpuser);

                            if (depth < maxdepth) handleUser1(null, tmpuser.ScreenName, targetfile, depth + 1, maxdepth, output);
                        }
                    }
                }
                catch (Exception)
                { }
                */
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
                            getTwitterNode(favoredtweet.CreatedBy);

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
                            getTwitterNode(retweet.CreatedBy);

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

        public static void searchTerm(Dictionary<string, int> alreadyProcessed, String term, String targetfile, int depth, int maxdepth, int output)
        {
            //int maxdepth = 1;
            try
            {
                var searchParameter = new SearchTweetsParameters(term)
                {
                    //GeoCode = new GeoCode(-122.398720, 37.781157, 1, DistanceMeasure.Miles),
                    //Lang = LanguageFilter.English,
                    //SearchType = SearchResultType.Popular,
                    MaximumNumberOfResults = 100,
                    //Until = new DateTime(2015, 06, 02),
                    //SinceId = 399616835892781056,
                    //MaxId = 405001488843284480,
                    //Filters = TweetSearchFilters.Images | TweetSearchFilters.Verified
                };
                var tweets = Tweetinvi.Search.SearchTweets(searchParameter).ToList();
                var tweetList = new List<ITweet>(tweets);
                Console.WriteLine(tweetList.Count + " tweets found");

                //IUser user = null;

                foreach (ITweet tweet in tweetList)
                {
                    if (!alreadyProcessed.ContainsKey(tweet.CreatedBy.ScreenName))
                    {
                        handleUser1(term, tweet.CreatedBy.ScreenName, targetfile, depth, maxdepth, output);
                        alreadyProcessed.Add(tweet.CreatedBy.ScreenName, 1);
                    }
                    else
                    {
                        int tmpval;
                        alreadyProcessed.TryGetValue(tweet.CreatedBy.ScreenName, out tmpval);
                        alreadyProcessed.Remove(tweet.CreatedBy.ScreenName);
                        alreadyProcessed.Add(tweet.CreatedBy.ScreenName, tmpval + 1);
                    }
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

        public static String getYoutubeUser(String id)
        {
            String key = @"&key=" + File.ReadAllText(@"C:\TwitterSearch2Gephi" + @"\youtube.txt");
            // Id, Label, Interval [, ...]
            // COLLECT DATA FROM YOUTUBE CHANNEL
            // https://developers.google.com/youtube/v3/docs/channels
            // https://www.googleapis.com/youtube/v3/channels?part=snippet%2CcontentDetails%2Cstatistics%2CauditDetails%2CcontentOwnerDetails%2Clocalizations%2Cstatus
            //String username = "GoogleDevelopers";
            //String usernamestr = "&forUsername=" + username;
            //String urlChannels = "https://www.googleapis.com/youtube/v3/channels?part=snippet%2CcontentDetails%2Cstatistics%2CcontentOwnerDetails" + usernamestr + key;
            String idstr = "&id=" + id;
            String urlChannels = "https://www.googleapis.com/youtube/v3/channels?part=snippet%2CcontentDetails%2Cstatistics%2CcontentOwnerDetails" + idstr + key;

            MyWebClient mwc = new MyWebClient();
            String strres = mwc.DownloadString(urlChannels);
            Console.WriteLine(strres);

            JObject jsonresu = JObject.Parse(strres);
            JArray itemsu = (JArray)jsonresu["items"];
            String res = "";

            foreach (var item in itemsu)
            {
                res += item["id"];
                res += ",\"" + stripString((String)item["snippet"]["title"]) + "\"";
                res += "," + item["snippet"]["publishedAt"]; 
                res += ",\"" + stripString((String)item["snippet"]["description"]) + "\"";
                res += "," + item["snippet"]["customUrl"];
                res += "," + item["snippet"]["defaultLanguage"];
                res += "," + item["snippet"]["country"];
                res += "," + item["statistics"]["viewCount"];
                res += "," + item["statistics"]["subscriberCount"];
                res += "," + item["statistics"]["hiddenSubscriberCount"];
                res += "," + item["statistics"]["videoCount"];
                return res;

                Console.WriteLine("id: " + item["id"]);
                Console.WriteLine("snippet.title: " + item["snippet"]["title"]);
                Console.WriteLine("snippet.publishedAt: " + item["snippet"]["publishedAt"]); 
                Console.WriteLine("snippet.description: " + item["snippet"]["description"]);
                Console.WriteLine("snippet.customUrl: " + item["snippet"]["customUrl"]);
                Console.WriteLine("snippet.defaultLanguage: " + item["snippet"]["defaultLanguage"]);
                Console.WriteLine("snippet.country: " + item["snippet"]["country"]);
                Console.WriteLine("statistics.viewCount: " + item["statistics"]["viewCount"]);
                Console.WriteLine("statistics.subscriberCount: " + item["statistics"]["subscriberCount"]);
                Console.WriteLine("statistics.hiddenSubscriberCount: " + item["statistics"]["hiddenSubscriberCount"]);
                Console.WriteLine("statistics.videoCount: " + item["statistics"]["videoCount"]);
                //Console.WriteLine("auditDetails.overallGoodStanding: " + item["auditDetails"]["overallGoodStanding"]);
                //Console.WriteLine("contentOwnerDetails.contentOwner: " + item["contentOwnerDetails"]["contentOwner"]);

            }
            return "";
        }

        static void Main(string[] args)
        //static async Task Main(string[] args)
        {
            String outdir = @"C:\TwitterSearch2Gephi";
            String outfile = outdir + @"\edges.csv";
            String nodesoutfile = outdir + @"\nodes.csv";
            String ewfile = outdir + @"\edges-weighted.csv";
            String accountsfile = outdir + @"\accounts.txt";
            String domainsfile = outdir + @"\domains.txt";
            String searchtermsfile = outdir + @"\searchterms.txt";
            String credentialsfile = outdir + @"\credentials.txt";
            String subredditsfile = outdir + @"\subreddits.txt";
            String redditcredentialsfile = outdir + @"\redditcredentials.txt";
            String userchoice = "";
            ConsoleKeyInfo choice;

            Console.WriteLine("TwitterSearch2Gephi - @DisinfoG");
            Console.WriteLine("https://twitter.com/DisinfoG \n");
            Console.WriteLine("https://github.com/hjunker/TwitterSearch2Gephi");
            Console.WriteLine("using folder " + outdir);
            Console.WriteLine("");
            Console.WriteLine("\ntwitter\n-------");
            Console.WriteLine("a - crawl twitter accounts from accounts.txt and create edges.csv and nodes.csv");
            Console.WriteLine("s - collect twitter data from search terms in searchterms.txt and create edges.csv and nodes.csv");
            Console.WriteLine("\nwww\n---");
            Console.WriteLine("d - crawl web domains / URLs from domains.txt and create edges.csv");
            Console.WriteLine("\nreddit\n------");
            Console.WriteLine("r - crawl reddit subreddits from subreddits.txt / accounts from redditaccounts.txt and create edges.csv");
            Console.WriteLine("\nyoutube\n------");
            Console.WriteLine("y - collect youtube data from search terms in searchtermsn.txt and create edges.csv");
            Console.WriteLine("\ngeneral\n-------");
            Console.WriteLine("w - create weighted edges file (from edges.csv to edges-weighted.csv)");
            Console.WriteLine("u - create edges-ucinet.csv from edges.csv in order to make it usable in ucinet");
            Console.WriteLine("c - clique-analysis from edges-weighted.csv (early stage PoC)");
            Console.WriteLine("m - adjacency matrix test (early & slow stage PoC, obsolete when ucinet support is included");
            Console.Write("\nWhat should I do for you? ");
            choice = Console.ReadKey();
            Console.WriteLine("\n");
            userchoice += choice.KeyChar;

            if (choice.KeyChar == 'u')
            {
                Console.WriteLine("converting edges.csv to edges-ucinet.csv");

                //File.WriteAllText(outdir + @"\edges-ucinet.csv", "Source,Target,Kind\n");

                Dictionary<string, int> edges = new Dictionary<string, int>();

                String[] lines = File.ReadAllLines(outfile);

                Char dataset = 'w';
                
                for (int i = 1; i < lines.Length; i++)
                {
                    String[] values = lines[i].Split(',');

                    writeLineUci(values[0], values[1], values[3], outdir + @"\edges-ucinet.csv");
                }

                Console.WriteLine("\n\nyou can now use edges-ucinet.csv to create a xlsx file which can then be imported in ucinet.");
            }

            // -------------------------------------------

            if (choice.KeyChar == 'm')
            {
                String[] nodes = { "", "MitBenutzername", "soistdatt", "p_manske", "PeterBorbe", "ThePraetorian21", "jan_mainka", "guenter0853", "politischerBeo1", "MeisnerWerner", "Itschi1", "HellerNorden", "mumaemar", "Kra07Man", "McGaybear", "RalfSchmitz2402", "Stephen12Miller", "WunderEmil", "SharleenM5", "Procyon25", "LudwigLarcher", "Inhaber1", "schamolf666", "schweizok2", "SapmiThe", "Jane_Banane_", "PPiwi55", "Mutbrger3", "MichaelRigol", "JensJahr", "Higgswielange", "sanvenganz_a", "orwell1984_a", "GNaktiv", "Panthersprung", "Zigeunerbaron3", "RuthBroucq", "LuckyLandShop", "Walthander", "Xena93795210", "OfficeRolando", "MaTiaWa1", "sifu_Qkung_fu", "OlivierKahn3", "Qnessi17", "pimmelbob", "sgasteyger", "lookintothesky3", "pjensen111", "tioneada", "mythologous", "schmidja2017", "MarcAnt0463", "oldipeterpan", "HPunkt_SPunkt", "wollea71", "SchwarzeHexe5", "Maxman23", "Schnapp98712747", "Stephan2301", "Petra507838451", "St_MichaelAngel", "nertha7", "Mirola11695965", "rheindelta20", "Lipitelektroph1", "MHHofmeister", "martinm90350202", "SnaH3210", "Q_Tweets", "MS87366367", "Starwalker999", "michacsendc", "Vaaltor", "Peter73832399", "LedbetterRalf", "PratoriusDr", "Northpoleshift", "strfry", "Muelle2s", "VolksArchitekt", "nimrod63", "marcusneuhaus", "RasantiVeloce", "Nebulous81", "TheTrueMPK", "Simon_Groh", "ZettJens", "von_kries", "TS_Quint", "Liraja5", "Imageberatungen", "Tschonka", "tlehr15", "suerprisli", "UlrHenn", "Jaybird_XXX", "uwe_1955", "JosephinaLopz1", "politisch_und_k" };
                String[,] adjmatrix = new string[nodes.Length, nodes.Length];
                for (int i = 0; i<nodes.Length; i++)
                {
                    adjmatrix[0, i] = nodes[i];
                    adjmatrix[i, 0] = nodes[i];
                }

                for (int i = 0; i<nodes.Length; i++)
                {
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        if ((i!=0) & (j!=0) & (i!=j))
                            adjmatrix[i, j] = ""+0;
                    }
                }

                String[] edges = File.ReadAllLines(outfile);

                foreach (String edge in edges)
                {
                    String[] edgevalues = edge.Split(',');
                    String nodeA = edgevalues[0].Substring(1);
                    String nodeB = edgevalues[1].Substring(1);
                    //Console.WriteLine(nodeA + " / " + nodeB);

                    for (int i = 0; i < nodes.Length; i++)
                    {
                        String username = nodes[i];
                        
                        for (int j = 0; j < nodes.Length; j++)
                        {
                            if ((i != 0) & (j != 0) & (i != j))
                            {
                                String username2 = nodes[j];
                                
                                if (((nodeA.Equals(username)) & (nodeB.Equals(username2))))
                                //if ((edge.Contains(username)) & (edge.Contains(username2)))
                                {
                                    adjmatrix[i, j] = "" + 1;
                                    Console.WriteLine(username + "," + username2 + "," + edgevalues[3]);
                                }
                                else
                                {
                                    //adjmatrix[i, j] = "" + 0;
                                }
                                /*
                                if (((nodeB.Equals(username)) & (nodeA.Equals(username2))))
                                {
                                    adjmatrix[j, i] = "" + 1;
                                    Console.WriteLine(username2 + "," + username + "," + edgevalues[3]);
                                }
                                else
                                {
                                    //adjmatrix[j, i] = "" + 0;
                                }
                                */
                            }
                            if (i == j) adjmatrix[i, j] = "";
                        }
                    }
                }

                for (int i = 0; i < nodes.Length; i++)
                {
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        Console.Write(adjmatrix[i, j] + ",");
                    }
                    Console.Write("\n");
                    }
                }

            // ------------------------------------------------

            if (choice.KeyChar == 'c')
            {
                String nodeA, nodeB;
                // patient zero account(s) P0
                String p0 = "guenter0853";


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


                // TODO: better performance when exchanging the first two foreach loops?!
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
                    Console.WriteLine("processing search term " + term);
                    searchTerm(processedAccounts, term, outfile, 0, maxdepth, output);
                }

                foreach (var stat in processedAccounts)
                {
                    Console.WriteLine(stat.Key + "," + stat.Value);
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
                File.WriteAllText(nodesoutfile, "Id,Label,timeset,twitter_type,lat,lng,place_country,place_type,place_fullname,place_name,created_at,lang,possibly_sensitive,quoted_status_permalink,description,email,profile_image,friends_count,followers_count,real_name,location,emoji_alias,emoji_html_decimal,emoji_utf8\n");
                //File.WriteAllText(nodesoutfile, "Id,Label,friends_count,followers_count\n");

                // MANUAL MAPPING OF ACCOUNTS, e.g. RTDeutsch and russiatoday, also for cross social-media mapping, ...
                /*
                DateTime timeset = DateTime.Now;
                writeLine(timeset, "HolgerJunker", "DisinfoG", "RetweetedBy", outfile, 50);
                */

                Console.WriteLine("Starting to collect engagements data from accounts...");

                foreach (String handle in accounts)
                {
                    handleUser1(null, handle, outfile, 0, maxdepth, output);
                }
            }

            // --------------------------------------------------------------

            if (choice.KeyChar == 'y')
            {
                int maxdepth = 1;
                int output = 1;
                MyWebClient mwc = new MyWebClient();

                Console.WriteLine("youtube search");

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

                String key = @"&key=" + File.ReadAllText(outdir + @"\youtube.txt");
                JObject jsonres;
                String strres = "";
                JArray items_video, items_comment, items_replies;
                String publishedAfter = "&publishedAfter=2020-10-01T00%3A00%3A00Z";
                String urlSearchList;
                String userinfo = "";
                Dictionary<string, string> ytusers = new Dictionary<string, string>();

                File.WriteAllText(outfile, "Source,Target,Type,Kind,Id,Label,timeset,Weight\n");
                File.WriteAllText(nodesoutfile, "Id,Label,Interval,Description,Url,Language,Country,ViewCount,SubscribersCount,HiddenSubscribersCount,VideoCount\n");
                
                // TODO: handle paginated results for keyword search (similar to the way it is already implemented for comments)
                foreach (String querywords in searchterms)
                {
                    String query = "&q=" + querywords;
                    Console.WriteLine("searching for " + querywords);
                    urlSearchList = "https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=100" + query + publishedAfter + key;

                    strres = mwc.DownloadString(urlSearchList);
                    //Console.WriteLine(strres);

                    jsonres = JObject.Parse(strres);
                    items_video = (JArray)jsonres["items"];

                    Console.WriteLine("videos for search query: " + querywords);
                    foreach (JObject tmp in items_video)
                    {
                        Boolean firstpage = true;
                        String nextPageToken = null;

                        // nur Videos --> tmp["id"]["kind"] == "youtube#video"
                        Console.WriteLine(tmp["snippet"]["title"] + " / " + tmp["snippet"]["channelId"] + " / " + tmp["id"]["videoId"]);
                        //Console.WriteLine(tmp["replies"]);

                        // searchTerm(processedAccounts, term, outfile, 0, maxdepth, output);
                        DateTime timeset = DateTime.Now;
                        String title = "[VIDEO]" + ((String)tmp["snippet"]["title"]).Replace(',','_');
                        String channelTitle = ((String)tmp["snippet"]["channelTitle"]).Replace(',', '_');
                        writeLine('w', timeset, "" + tmp["snippet"]["channelId"], title + " / " + tmp["id"]["videoId"], "publishedVideo", outfile, 10);

                        //userinfo = getYoutubeUser((String)tmp["snippet"]["channelId"]);
                        //File.AppendAllText(nodesoutfile, userinfo + "\n");
                        //Console.WriteLine("USERINFO: " + userinfo);
                        try
                        {
                            ytusers.Add((String)tmp["snippet"]["channelId"], (String)tmp["snippet"]["channelId"]);
                        }
                        catch (Exception e)
                        { }

                        // get relations from comments on current video
                        while ((firstpage == true) | (nextPageToken != null))
                        {
                            //Console.WriteLine("next iteration...");
                            String maxres = @"&maxResults=100";
                            String videoid = "" + tmp["id"]["videoId"];
                            String video = @"&videoId=" + videoid;
                            String urlcommentThreadList = @"https://www.googleapis.com/youtube/v3/commentThreads?part=snippet%2Creplies" + maxres + video + key;
                            if (nextPageToken != null)
                            {
                                urlcommentThreadList += "&pageToken=" + nextPageToken;
                            }
                            //Console.WriteLine("ThreadList URL: " + urlcommentThreadList);

                            // TODO: handle various cases that might occur, such as disabled comments
                            try
                            {
                                mwc = new MyWebClient();
                                strres = mwc.DownloadString(urlcommentThreadList);
                                //Console.WriteLine(strres);

                                jsonres = JObject.Parse(strres);
                                nextPageToken = (String)jsonres["nextPageToken"];
                                items_comment = (JArray)jsonres["items"];

                                Console.WriteLine("comments to video " + videoid);
                                foreach (JObject item in items_comment)
                                {
                                    String authorDisplayName = ((String)item["snippet"]["topLevelComment"]["snippet"]["authorDisplayName"]).Replace(',', '_');
                                    String textOriginal = (String)item["snippet"]["topLevelComment"]["snippet"]["textOriginal"];
                                    String authorChannelUrl = (String)item["snippet"]["topLevelComment"]["snippet"]["authorChannelUrl"];
                                    String authorChannelId = (String)item["snippet"]["topLevelComment"]["snippet"]["authorChannelId"]["value"];
                                    Console.WriteLine(authorDisplayName + " / " + textOriginal + " / " + authorChannelUrl + " / " + authorChannelId);
                                    writeLine('w', timeset, "" + authorChannelId, title + " / " + videoid, "commentedVideo", outfile, 5);

                                    //userinfo = getYoutubeUser((String)item["snippet"]["topLevelComment"]["snippet"]["authorChannelId"]["value"]);
                                    //File.AppendAllText(nodesoutfile, userinfo + "\n");
                                    //Console.WriteLine("USERINFO: " + userinfo);
                                    try
                                    {
                                        ytusers.Add((String)item["snippet"]["topLevelComment"]["snippet"]["authorChannelId"]["value"], (String)item["snippet"]["topLevelComment"]["snippet"]["authorChannelId"]["value"]);
                                    }
                                    catch (Exception)
                                    { }

                                    // TODO: handle replies to comments
                                    //Console.WriteLine(item["replies"]["comments"]);
                                    if (item["replies"] != null)
                                    {
                                        //Console.WriteLine("REPLIES FOUND!!!");
                                        //Console.WriteLine(item["replies"]["comments"]);
                                        try
                                        {
                                            //JObject jsonreplies = JObject.Parse((String)item["replies"]["comments"]);
                                            items_replies = (JArray)item["replies"]["comments"];
                                            //Console.WriteLine("items_replies count: " + items_replies.Count);

                                            foreach (JObject item_reply in items_replies)
                                            {
                                                String replAuthorDisplayName = ((String)item_reply["snippet"]["authorDisplayName"]).Replace(',', '_');
                                                String replTextOriginal = (String)item_reply["snippet"]["textOriginal"];
                                                String replAuthorChannelUrl = (String)item_reply["snippet"]["authorChannelUrl"];
                                                String replAuthorChannelId = (String)item_reply["snippet"]["authorChannelId"]["value"];
                                                Console.WriteLine(replAuthorDisplayName + " / " + replTextOriginal + " / " + replAuthorChannelUrl + " / " + replAuthorChannelId);
                                                writeLine('w', timeset, "" + replAuthorChannelId, "" + authorChannelId, "repliedComment", outfile, 5);

                                                //userinfo = getYoutubeUser((String)item_reply["snippet"]["authorChannelId"]["value"]);
                                                //File.AppendAllText(nodesoutfile, userinfo + "\n");
                                                //Console.WriteLine("USERINFO: " + userinfo);
                                                try
                                                {
                                                    ytusers.Add((String)item_reply["snippet"]["authorChannelId"]["value"], (String)item_reply["snippet"]["authorChannelId"]["value"]);
                                                }
                                                catch (Exception)
                                                { }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("ERROR during handling REPLIES:\n" + e.Message + "\n" + e.StackTrace);
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                //Console.WriteLine("ERROR during handling ThreadList:\n" + urlcommentThreadList + "\n" + e.Message + "\n" + e.StackTrace);
                            }

                            if (firstpage == true)
                            {
                                firstpage = false;
                            }
                            
                            //Console.WriteLine("firstpage: " + firstpage);
                            //Console.WriteLine("nextPageToken: " + nextPageToken);
                        }
                    }
                }

                // TODO: make this optional since it might cause problems with rate limits
                // at the end of data collection for edges, create separate nodes.csv file with detailed node information
                foreach (KeyValuePair<string, string> tmp in ytusers)
                {
                    userinfo = getYoutubeUser(tmp.Key);
                    File.AppendAllText(nodesoutfile, userinfo + "\n");
                    //Console.WriteLine("USERINFO: " + userinfo);
                }
                
            }

            // --------------------------------------------------------------

            if (choice.KeyChar == 'r')
            {
                String[] credentials = null;
                try
                {
                    credentials = File.ReadAllLines(redditcredentialsfile);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error reading " + redditcredentialsfile + ". Please remember the file needs to contain the 3 credential parameters for your reddit account / app one per line in the order appId, refreshToken, accessToken. These tokens have to be generated with a third party component such as Reddit.NET. If you get an 401 error you need to renew your credentials.");
                    Console.ReadKey();
                }
                Reddit.RedditClient reddit = new Reddit.RedditClient(appId: credentials[0], refreshToken: credentials[1], accessToken: credentials[2]);
                DateTime timesetdummy = DateTime.Now;
                
                File.WriteAllText(outfile, "RSource,RTarget,Type,Kind,Id,Label,timeset,Weight\n");

                String[] subnames = File.ReadAllLines(subredditsfile);

                Console.WriteLine("processing subreddits");

                foreach (String subname in subnames)
                {
                    Console.WriteLine(subname);
                    try
                    {
                        List<Reddit.Controllers.Structures.Moderator> moderators = reddit.Subreddit(subname).Moderators;

                        Reddit.Controllers.Subreddit sub = reddit.Subreddit(subname).About();

                        foreach (Reddit.Controllers.Structures.Moderator tmp in moderators)
                        {
                            writeLine('w', timesetdummy, "u_" + tmp.Name, "r_" + sub.Name, "ModeratorOf", outfile, 2);
                        }

                        // Get new posts from this subreddit.
                        List<Reddit.Controllers.Post> newPosts = sub.Posts.New;

                        //Console.WriteLine("Retrieved " + newPosts.Count.ToString() + " new posts.\n\n");
                        foreach (var post in newPosts)
                        {
                            if (true)//post.Created > DateTime.Now.AddDays(-7))
                            {
                                //Console.WriteLine(post.Created.ToLongDateString() + " - " + post.Permalink + " - " + post.Subreddit + " / " + post.Title + " / " + post.Author + " / " + post.Fullname + " / " + post.Listing.SelfText);
                                writeLine('w', timesetdummy, "u_" + post.Author, "r_" + sub.Name, "PostedIn", outfile, 2);

                                foreach (Reddit.Controllers.Comment comment in post.Comments.GetComments())
                                {
                                    // TODOs: recursive crawling of comments, spam and other properties, created, awards, score, 
                                    //Console.WriteLine("comment by " + comment.Author + " / " + comment.Fullname + ": " + comment.Body + "[" + comment.UpVotes + "/" + comment.DownVotes + "]" + "(" + comment.Permalink + " / " + comment.Id + ")");
                                    writeLine('w', timesetdummy, "u_" + comment.Author, "u_" + post.Author, "CommentedOn", outfile, 2);
                                }
                                //Console.WriteLine("\n\n-----------------------------------------------------------------\n\n");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(" ! error !");
                    }
                    //System.Threading.Thread.Sleep(1000 * 60);
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
                if (lines[0].Equals("RSource,RTarget,Type,Kind,Id,Label,timeset,Weight")) dataset = 'w';

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
