# PBSMS v1 (January 27, 2026)

PBSMS is a free open source Windows® Command Line Interface program for sending text messages using [Pushbullet](https://www.pushbullet.com/)®.

## Key Features

- Sends text messages from the Windows command line
- The Pushbullet API Key only needs to be provided once and it will be securely saved as outlined [here](https://github.com/roblatour/PBSMS/blob/main/PBPSMSAPIKeySecurity.md) - with no need to include it in batch/script files


## Setup and usage

1. Download the PBSMS program (in a zip file) from [here](https://github.com/roblatour/PBSMS/releases/tag/v1.0.0.0).
2. Extract the  program from the downloaded zip file.
3. (Optionally) create a new folder from which you would like the program to run. 
4. Copy the executable program (pbsms.exe) extracted in step 2 to the folder from which you would like it to run.
5. At the Windows Command prompt, from the folder that you copied the program into, enter pbsms to see the help.<br>
   ```
   pbsms
   ```

6. Next, in order for PBSMS to be able to send a text it must first be provided with a Pushbullet API key.<br>
   ```
   pbsms APIKey=o.abc1def2ghi3klm5mno6pqr7stu8vwx9
   ```
   Later, if the API Key changes simply re-issue the above command using the new API key.
7. To send a text message provide the program with the destination phone number and message.<br>
   ```
   pbsms +15551234567 "Yeah, I won the Lotto!"
   ```
8. (Optionally) Use the program in a batch / script file to access its return code.
    Here is an example batch file (.bat):
    ```batch
    @echo off
    "c:\Program Files\PBSMS\pbsms" +15551234567 "Hello World"
    echo Return code: %ERRORLEVEL%
    pause
    ```
9. (Optionally) To remove (delete) the API key enter:
   ```
   pbsms APIKey=remove
   ```

## License
PBSMS is licensed under the [MIT license](https://github.com/roblatour/PBSMS/blob/main/LICENSE)


* * *
 ## Support Send via Outlook Classic

 To help support Send via Outlook Classic, or to just say thanks, you're welcome to 'buy me a coffee'<br><br>
[<img alt="buy me  a coffee" width="200px" src="https://cdn.buymeacoffee.com/buttons/v2/default-blue.png" />](https://www.buymeacoffee.com/roblatour)
* * *
Copyright © 2026, Rob Latour
* * *
