This project demonstrates how to use the intaker library in a Console app.

## Workflow:

This Console app reads a data file and uses a file specification file to decode the data file and generates a json file with the decoded data.

Argument | Description
-|-
specs | Path of XML file with the file specification used when parsing a data file.
input | Path of CSV file with the data to be parsed.
output | Path of JSON file that is created with the data decoded from the data file.

&nbsp;  
File `launchSettings.json` includes some pre-configured examples using files from folder `sample-files` 
