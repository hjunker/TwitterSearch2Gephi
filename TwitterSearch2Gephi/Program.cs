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

namespace TwitterSearch2Gephi
{
    class Program
    {
        public static int counter = 0;

        public static void writeLine(DateTime dt, String nameA, String nameB, String kind, String targetfile, int weight)
        {
            String timeset = dt.ToUniversalTime().ToString("u");
            timeset = "<[" + timeset.Replace(' ', 'T') + "]>";
            String res = "@" + nameA + ",@" + nameB + ",Directed," + kind + "," + counter + ",," + timeset + "," + weight;
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

        public static async void handleUser1(String handle, String targetfile, int depth, int maxdepth, int output)
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
                    System.Collections.Generic.IEnumerable<IUser> enumfollowers = user.GetFollowers(250);
                    Console.WriteLine("followers found: " + enumfollowers.Count());
                    if (enumfollowers != null)
                    {
                        foreach (IUser tmpuser in enumfollowers)
                        {
                            DateTime timeset = tmpuser.CreatedAt;

                            if (output == 1)
                            {
                                writeLine(timeset, user.ScreenName, tmpuser.ScreenName, "FollowedBy", targetfile, 2);
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
                    System.Collections.Generic.IEnumerable<IUser> enumfriends = user.GetFriends(250);
                    if (enumfriends != null)
                    {
                        foreach (IUser tmpuser in enumfriends)
                        {
                            DateTime timeset = tmpuser.CreatedAt;

                            if (output == 1)
                            {
                                writeLine(timeset, tmpuser.ScreenName, user.ScreenName, "FriendOf", targetfile, 5);
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
                    System.Collections.Generic.IEnumerable<ITweet> favtweets = user.GetFavorites(40);
                    if (favtweets != null)
                    {
                        foreach (ITweet favoredtweet in favtweets)
                        {
                            DateTime timeset = favoredtweet.CreatedAt;
                            
                            if (output == 1)
                            {
                                writeLine(timeset, favoredtweet.CreatedBy.ScreenName, user.ScreenName, "FavoredBy", targetfile, 1);
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
                                writeLine(timeset, retweet.CreatedBy.ScreenName, user.ScreenName, "RetweetedBy", targetfile, 2);
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
                System.Threading.Thread.Sleep(1000 * 4);
                

                //user.TweetsRetweetedByFollowers;
            }
            catch (Exception)
            {
                Console.WriteLine("error during processing of user " + handle.ToString());
            }
        }

        static void Main(string[] args)
        //static async Task Main(string[] args)
        {
            String outdir = @"C:\TwitterSearch2Gephi";
            String outfile = outdir + @"\edges.csv";
            String accountsfile = outdir + @"\accounts.txt";
            String credentialsfile = outdir + @"\credentials.txt";

            Console.WriteLine("TwitterSearch2Gephi - @DisinfoG");
            Console.WriteLine("https://twitter.com/DisinfoG \n");
            Console.WriteLine("https://github.com/hjunker/TwitterSearch2Gephi");
            Console.WriteLine("using folder " + outdir);
            Console.WriteLine("");
            Console.WriteLine("What should I do for you?");
            Console.WriteLine("c - crawl accounts from accounts.txt and create edges.csv");
            Console.WriteLine("w - create weighted edges file (from edges.csv to edges-weighted.csv)");

            ConsoleKeyInfo choice = Console.ReadKey();

            if (choice.KeyChar == 'c')
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
                    writeLine(timesetdummy, accounts[0].Substring(1), accounts[1].Substring(1), "aggregated", outdir + @"\edges-weighted.csv", tmp.Value);
                }
            }

                // --------------------------------------------------------------

                Console.WriteLine("FINISHED. Hit key to exit.");
            Console.ReadKey();
        }
    }
}
