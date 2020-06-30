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

namespace TwitterSearch2Gephi
{
    class Program
    {
        public static int counter = 0;

        public static void writeLine(DateTime dt, String nameA, String nameB, String kind, String targetfile)
        {
            String timeset = dt.ToUniversalTime().ToString("u");
            timeset = "<[" + timeset.Replace(' ', 'T') + "]>";
            String res = "@" + nameA + ",@" + nameB + ",Directed," + kind + "," + counter + ",," + timeset + ",1";
            File.AppendAllText(targetfile, res + "\n");
            counter++;
        }

        public static void handleUser1(String handle, String targetfile, int depth, int maxdepth)
        {
            //int maxdepth = 1;
            try
            {
                IUser user = User.GetUserFromScreenName(handle);
                if (user == null) Console.WriteLine("ERROR GETTING USER");
                Console.WriteLine(user.ScreenName + " / " + user.Name + " / " + user.IdStr);

                try
                {
                    System.Collections.Generic.IEnumerable<IUser> enumfollowers = user.GetFollowers(250);
                    if (enumfollowers != null)
                    {
                        foreach (IUser tmpuser in enumfollowers)
                        {
                            DateTime timeset = tmpuser.CreatedAt;
                            writeLine(timeset, user.ScreenName, tmpuser.ScreenName, "FollowedBy", targetfile);

                            if (depth < maxdepth) handleUser1(tmpuser.ScreenName, targetfile, depth + 1, maxdepth);
                        }
                    }
                }
                catch (Exception)
                { }

                try
                {
                    System.Collections.Generic.IEnumerable<IUser> enumfriends = user.GetFriends(250);
                    if (enumfriends != null)
                    {
                        foreach (IUser tmpuser in enumfriends)
                        {
                            DateTime timeset = tmpuser.CreatedAt;
                            writeLine(timeset, tmpuser.ScreenName, user.ScreenName, "FriendOf", targetfile);

                            if (depth < maxdepth) handleUser1(tmpuser.ScreenName, targetfile, depth + 1, maxdepth);
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
                            writeLine(timeset, favoredtweet.CreatedBy.ScreenName, user.ScreenName, "FavoredBy", targetfile);

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
                            writeLine(timeset, retweet.CreatedBy.ScreenName, user.ScreenName, "RetweetedBy", targetfile);

                            //if (depth < maxdepth) handleUser1(favoredtweet.CreatedBy.ScreenName, targetfile, depth + 1);
                        }
                    }
                }
                catch (Exception)
                { }

                //counter++;
                System.Threading.Thread.Sleep(1000 * 2);

                //user.TweetsRetweetedByFollowers;
            }
            catch (Exception)
            {
                Console.WriteLine("error during processing of user " + handle.ToString());
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("TwitterSearch2Gephi - @DisinfoG");
            Console.WriteLine("https://twitter.com/DisinfoG \n");
            Console.WriteLine("");
            Console.WriteLine("");

            String outdir = @"C:\TwitterSearch2Gephi";
            String outfile = outdir + @"\edges.csv";
            String accountsfile = outdir + @"\accounts.txt";
            String credentialsfile = outdir + @"\credentials.txt";
            int maxdepth = 1;
            try
            {
                maxdepth = int.Parse(args[1]);
            }
            catch (Exception)
            {
                Console.WriteLine("no maxdepth given as first parameter. setting maxdepth = 1\n");
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

            Console.WriteLine("Starting to collect engagements data from accounts...");

            foreach (String handle in accounts)
            {
                handleUser1(handle, outfile, 0, maxdepth);
            }

            Console.WriteLine("FINISHED. Hit key to exit.");
            Console.ReadKey();
        }
    }
}
