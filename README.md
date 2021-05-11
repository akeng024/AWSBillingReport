# AWSBillingReport

AWSの利用コストを把握するため、AWSの請求レポートを毎日Slackに通知するLambda関数

2日前に使用したサービスとコストが通知される。  

#### コストが0円だった場合

![slack-good](image\slack-good.PNG)

#### コストが1円でもかかった場合

![slack-danger](image\slack-danger.PNG)
