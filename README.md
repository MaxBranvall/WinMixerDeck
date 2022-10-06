# WinMixerDeck

Stream Deck plugin which allows users to control the volume of individual applications from their Stream Deck. The backend utilizes my C# Stream Deck API wrapper, [StreamDeckCS](https://github.com/MaxBranvall/StreamDeckCS).

## Example



## Installation
How to install plugin
1. Download the .sdPlugin file from releases section of the repo
2. It will now be available under the Branflake category on your Stream Deck

## Getting Started
1. Place an Application Picker button on the Stream Deck, either select an app from property inspector, or click the button to select an app.
2. Place a Volume Controller button directly above and directly below the app picker button. Assign each controller to volume up or volume down.
3. Place a Volume Interval button somewhere and click it select a volume interval.
4. Enjoy!

## Supported Actions

1. Volume Interval <br/> 
Assigning this to a button allows users to change the interval an apps volume will be changed
by whenever a volume controller is pressed
   1. When the user presses and releases a Volume Interval button, the volume interval will be cycled up by one. Supported intervals are 0, 1, 2, 5, 10, 25.
2. Application Picker <br/>
Assigning this to a button allows users to assign any currently open application with an audio stream to it. Volume controllers link to these buttons in order to control their volume.
   1. When the user presses and releases an Application Picker button, the associated app will be changed to another available app. 
1. Volume Controller <br/>
Assigning this to a button either directly above an Application Picker button, or directly below, allows users to assign a volume up or volume down command to the app specified by the Application Picker button.

## Roadmap

- [ ] Change background of Application Picker to icon representing selected app
- [ ] Show current volume of app when volume is changed
- [ ] Change all action icons to something permanent and more representative of their actions
- [ ] Develop a custom profile which automatically assigns all currently open apps with an audio stream to a button. This will negate the need for users to individually place buttons for each app.
 
## Known Issues

 - Changing volume interval directly from Stream Deck property inspector does not apply changes
 - When volume interval button first appears, it is blank until user presses it at least once.
 - Default font size makes some app names unreadable due to overflow

<!-- CONTACT -->
## Contact

Max Branvall - maxbranvalldevelopment@gmail.com

Project Link: [https://github.com/maxbranvall/streamdeckcs](https://github.com/maxbranvall/streamdeckcs)


<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [Thanks othneildrew for the awesome ReadME template!](https://github.com/othneildrew/Best-README-Template)
