This project demonstrates how to use the intaker library in a Console app that writes to a DynamoDb table.

## Workflow:

This Console app reads a data file and uses a file specification file to decode the data file and insert the decoded data in a table.

Argument | Description
-|-
specs | Path of XML file with the file specification used when parsing a data file.
input | Path of CSV file with the data to be parsed.
table-name | Name of the DynamoDb table to copy the data.

&nbsp;  
File `launchSettings.json` includes some pre-configured examples using files from folder `sample-files` 

### Using DynamoDb in a local docker container.

Docker - Get image
>docker pull amazon/dynamodb-local

Docker - Run image
>docker run -d -p 8000:8000 amazon/dynamodb-local

Using AWS CLI connecting to the local container
--endpoint-url http://localhost:8000

List current tables (default AWS CLI profile)
>aws dynamodb list-tables --endpoint-url http://localhost:8000

List current tables (developer-tool profile)
>aws dynamodb list-tables --endpoint-url http://localhost:8000 --profile developer-tool

I will be using --profile developer-tool for this demo. The profile being used is important in dynamodb-local because tables created using one profile are not available to other profiles.

Create a table using a json (Execute inside DymanoDb folder)
>aws dynamodb create-table --cli-input-json file://file-table.json --endpoint-url http://localhost:8000 --profile developer-tool


Using UI dynamodb-admin for dynamodb-local
https://www.npmjs.com/package/dynamodb-admin

Using Linux. (See the ref for Windows)
>DYNAMO_ENDPOINT=http://localhost:8000 AWS_REGION=<region> AWS_ACCESS_KEY_ID=<access-key> AWS_SECRET_ACCESS_KEY=<secret-key> dynamodb-admin 

AWS_REGION, AWS_ACCESS_KEY_ID, and AWS_SECRET_ACCESS_KEY must have the same values used for AWS CLI profile.

Open
http://localhost:8001