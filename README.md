# README #

This Service provides centralized settings for all services with field restrictions


### How do I get set up? ###

* Create Azure Website
* Create Azure table storage account
* Set Application Settings: ConnectionString=[Storage Account Connection String]

### How to configure access to Settings? ###

Please, insert a record to table with name Tokens

#Format of the Record:#

* PartitionKey = "A"
* RowKey = "{AccessToken}"
* AccessList = List of json nodes token has access to
* IpList = List of Ip token access from

### AccessList Column ###

* If it's empty or contains "*" that means token has access to all json document;
* Format: {property1};{property2};{property3}
*  Wildcard "*" can be used;
* property1.\* - means, property with name [property1] with all sub-properties and values is show;
* property1   - means property1 and it's value only is shown. Eg. "propert1":"somevalue"

Example:

  Databases.\*;MatchinEngine.\*;SomeOtherFiled

### IpList Column ###
* If it's empty or contains "*" that means - all requests with all ip's has access to that token;
* Format: {ip1};{ip2};{ip3}
* Wildcard "*" can be used

Example:
  45.76.23.87;22.88.*;165.255.34.237

Means token has access only form Ip addresses in the list. As well 22.88.0.0 - 22.88.255.255 is in order;

### Json Access Url Format ###

https://myurl.net/{token}

### Use cases ###
https://lykkex.atlassian.net/wiki/spaces/LKEWALLET/pages/477691938/Settings+service+v2
