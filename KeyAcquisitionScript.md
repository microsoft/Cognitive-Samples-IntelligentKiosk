# Script to acquire cognitive services keys required for the Kiosk application using azure cli commands from bash / git bash

## Create resource group
 ```sh
 # Create resource group, replace resouce group name and location of resource group as required
 az group create -n kiosk-cog-service-keys -l westus

 ```

## Generate keys and echo the keys
Please note! **[jq](https://stedolan.github.io/jq/)** needs to be installed to execute the commands below. If you do not want to use jq then you can just execute the az group deployment command and then search in the outputs section of the json where you will find the keys.

### To get the keys with the default parameters execute the following commands
  ```sh
  # The command below creates the cognitive service keys required by the KIOSK app, and then prints the keys
  echo $(az group deployment create -n cog-keys-deploy -g kiosk-cog-service-keys --template-uri https://raw.githubusercontent.com/Microsoft/Cognitive-Samples-IntelligentKiosk/master/Kiosk/cognitive-keys-azure-deploy.json) | jq '.properties.outputs'

  # If you dont have jq installed you can execute the command, and manually search for the outputs section
  # az group deployment create -n cog-keys-deploy -g kiosk-cog-service-keys --template-uri https://raw.githubusercontent.com/Microsoft/Cognitive-Samples-IntelligentKiosk/master/Kiosk/cognitive-keys-azure-deploy.json

  ```

### If instead you want to modify the default parameters you need to get the cognitive-keys-azure-deploy.json and cognitive-keys-azure-deploy.parameters.json files locally and execute the following commands
 ```sh    
 # Change working directory to Kiosk
 cd Kiosk
   
 # The command below creates the cognitive service keys required by the KIOSK app, and then prints the keys. You can modifiy the tiers associated with the generated keys by modifying the parameter values
 echo $(az group deployment create -n cog-keys-deploy -g kiosk-cog-service-keys --template-file cognitive-keys-azure-deploy.json --parameters @cognitive-keys-azure-deploy.parameters.json) | jq '.properties.outputs'

 # If you dont have jq installed you can execute the command, and manually search for the outputs section
 # az group deployment create -n cog-keys-deploy -g kiosk-cog-service-keys --template-file cognitive-keys-azure-deploy.json --parameters @cognitive-keys-azure-deploy.parameters.json
     
 ```

### Sample output of above commands is as follows:
    
```json

# Sample output of above command
{
    "bingAugosuggestKey1": {
        "type": "String",
        "value": "cb4******************************"
    },
    "bingSearchKey1": {
        "type": "String",
        "value": "88*********************************"
    },
    "compVisionEndpoint": {
        "type": "String",
        "value": "https://westus.api.cognitive.microsoft.com/vision/v1.0"
    },
    "compVisionKey1": {
        "type": "String",
        "value": "fa5**************************************"
    },
    "faceEndpoint": {
        "type": "String",
        "value": "https://westus.api.cognitive.microsoft.com/face/v1.0"
    },
    "faceKey1": {
        "type": "String",
        "value": "87f7****************************************"
    },
    "textAnalyticsEndpoint": {
        "type": "String",
        "value": "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0"
    },
    "textAnalyticsKey1": {
        "type": "String",
        "value": "ba3*************************************"
    }
}

```
