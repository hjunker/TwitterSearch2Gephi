# TwitterSearch2Gephi
TwitterSearch2Gephi is a windows CLI app that lets you collect data from social media and convert it into a CSV data set that can be used with Gephi. Currently it supports
* Twitter
* Reddit
* Youtube
* WWW domains / URLs

More social media platforms and more features for the platforms already implemented will be added later.

Written by [@DisinfoG](https://twitter.com/DisinfoG)

## Installation
Currently there is no binary package available for TwitterSearch2Gephi on GitHub. As Obi Wan would say: May the source be with you. Currently TwitterSearch2Gephi does not intend to be a product (and it probably won't in the near future) - it is more like a PoC or something to get you started. Depending on what you want to do with it, you have to code yourself or contact me. Basic knowledge in coding (not necessarily .NET) and should be sufficient.

TwitterSearch2Gephi requires the folder C:\TwitterSearch2Gephi as its working directory where it can read and write files. In case you want to use TwitterSearch2Gephi with Mono, you have to adjust this (hard coded) directory. This should be enough to get you up and running with Mono since TwitterSearch2Gephi is a .NET-Core application (not a full .NET application).

![x](img/githubimg01.png)

The credentials for accessing the twitter REST API have to be placed in credentials.txt with one entry in each line for consumerKey, consumerSecret, userAccessToken, userAccessSecret in this order.

The credentials for accessing the Reddit API have to be placed in redditcredentials.txt. The file needs to contain the 3 credential parameters for your reddit account / app one per line in the order appId, refreshToken, accessToken. These tokens have to be generated with a third party component such as Reddit.NET. If you get an 401 error you need to renew your credentials - these tokens become obsolete within a narrow time window of non-usage.

The credential (API-Key! *not* OAuth2.0) for accessing the Youtube API have to be placed in youtube.txt. Refer to https://console.developers.google.com/apis/dashboard in order to get an API key.

## Usage
![x](img/githubimg00.png)
### Twitter (options a and s)
TwitterSearch2Gephi takes the accounts (ScreenName) to look for from accounts.txt (one ScreenName per line without @). For each account it collects the following for generating engagements data. Alternatively you can collect data using the search terms in searchterms.txt.
*	Followers (default max. 250)
*	Friends (default max. 250)
*	Favorites (default max. 40)
*	Retweets

Please note that ‘engagements’ refers to accounts engaging with another account or their tweets. The data is collected from twitter’s standard REST API, _not_ the engagements API.

Data collection can be performed recursive. Therefore TwitterSearch2Gephi will ask you for an integer to chose a maxdepth other than the default which is set to 1 for one iteration. It will also ask you whether to write the findings to file or send it live to gephi's web interface.

The tool is invoked by double-clicking TwitterSearch2Gephi.exe or starting it from a cmd shell. The output is written to edges.csv which can be imported to Gephi.
![x](img/githubimg02.png)

![x](img/githubimg03.png)
Gephi should automatically recognize the data in edges.csv as an edges table.
![x](img/githubimg04.png)
The default import setting can be used.
![x](img/githubimg05.png)
TwitterSearch2Gephi creates edges.csv which typically contains parallel edges. Use the option 'w' to convert edges.csv to weighted-edges.csv.
![x](img/githubimg06.png)
If everything works right you should see the data in the data lab view.
![x](img/githubimg07.png)
You can now visualize the data using the layouts you have installed in Gephi.
![x](img/githubimg08.png)

### Reddit (option r)
documentation coming soon

### Youtube (option y)
documentation coming soon

### WWW (option d)
documentation coming soon

### general (options w, u and c)
documentation coming soon

Option 'u' converts edges.csv to edges-ucinet.csv in order to make it usable in UCINET. edges-ucinet.csv has to be converted to a xslx file (e.g. with LibreOffice). The xslx file can then be imported with UCINET's DL editor (type edges23). You can then save the imported data as ##d and ##h files. Work on these files with UCINET.

## TODOs
Some things are not implemented yet.
* fix some issues on rate limits (currently TwitterSearch2Gephi does not always get that it is above rate limits and therefore does not process any engagements aka is skipping accounts)
*	Correct implementation of the timeset parameter
*	Handling of special characters, emojis, etc.
* documentation
*	…

## Contact
Feel free to give feedback or make feature requests here on GitHub or twitter (@DisinfoG / https://twitter.com/DisinfoG).

For services please refer to seculancer.de or DM @DisinfoG via Twitter.

## Contribute to TwitterSearch2Gephi
Please feel free to give a spare dime/dollar/euro/... or two... It will be used to further enhance my open source projects.
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=WLC2SHZL6SPNY)

You can also support my projects by donating hardware and other stuff to keep operative backend and development systems running. Please take a look at (my list on, [amazon](https://www.amazon.de/hz/wishlist/ls/2FD1Z75K43I7M?ref_=wl_share))
