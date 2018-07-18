# SSN Google Analytics

Connects to Google Analytics Reporting API to retrieve data and pushes it into SSN data storage.

Requires Google APIs project to be created via: [https://console.developers.google.com](https://console.developers.google.com)

Create service account and save .json credentials as `service-account.json` into `/GoogleAnalyticsReporter` directory.

Generate a .pfx file and put it into `/GoogleAnalyticsReporter` directory.

Service account's email must be granted access to SSN's Google Analytics [https://analytics.google.com](https://analytics.google.com) (account > admin > user management)

Zip the /bin/release/ directory and upload it as a webjob in live api's app service, running daily
