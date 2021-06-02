# Unity Watson Chat Demo

This Unity project is a demo. It demonstrates how to become a client and send data to the python server created in 'VRWatsonAgent'.

# Setup
To be able to use this demo you need to:
1. Import the project to unity
2. Set up your [IBM Bluemix](https://console.bluemix.net/registration/ "Bluemix") account
3. Create three services
   * Speech to Text
   * Conversation
   * Text To Speech
4. Configure your Conversation service with some intents
5. Open the Scenes\WatsonDemo scene and set the credentials to all the services in the Watson game object

# How to Use
Once all credentials are set simply run the Watson scene and the python server.

Once connected click (but do NOT hold) the "talk" button.
You will have 6 seconds to ask your question before a time-out. This number can be changed under the "Audio Handler" script settings
The data will be sent automatically and after a delay "Bob" will answer your question if he knows how to answer it, else the screen will prompt "No Response"

Repeat asking questions as much as needed
