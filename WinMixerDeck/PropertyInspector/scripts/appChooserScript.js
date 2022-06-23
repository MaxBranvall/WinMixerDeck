
import { Constants } from "./constants";

var websocket = null;
var PIuuid = null;
var uuid = null;
var actionInfo = {}
var keyFunction = null;
var appName = null;

// when willAppear fires, plugin will send coords to PI. PI will setSettings with coords of each key and what app they control as well as vol up or down
// Now when key is pressed, plugin can check coords against keys in settings and perform necessary action
// Then plugin will send new values to display to PI

// new: on change, set settings from PI using context and save volfunction and app. plugin will set title on willAppear

function connectElgatoStreamDeckSocket(inPort, inPropertyInspectorUUID, inRegisterEvent, inInfo, inActionInfo) {

    console.log("connecting");

    PIuuid = inPropertyInspectorUUID;
    actionInfo = JSON.parse(inActionInfo);
    websocket = new WebSocket('ws://localhost:' + inPort);
    
    websocket.onopen = function () {
        var json = {
         "event": inRegisterEvent,
         "uuid": inPropertyInspectorUUID
        };
       
        websocket.send(JSON.stringify(json));
    };

    websocket.onmessage = function (evt) {

        var msg = JSON.parse(evt['data']); 
        processMessage(msg);

    }

};

function processMessage(msg) {

    let event = msg['event']
    let messageType = msg['payload']['messageType']

    // get message from plugin
    if (event == Constants.SEND_TO_PI) {

        // store UUID of plugin, for use with custom settings
        uuid = msg['context'];

        // if plugin sent available audio sessions
        if (messageType == "getSessions") {

            // add option to select box for each audio session
            for (var i = 0; i < msg['payload']['audioSessions'].length; i++) {
                var option = document.createElement("option");
                option.value = msg['payload']['audioSessions'][i];
                option.innerHTML = msg['payload']['audioSessions'][i];

                document.getElementById("myselect").appendChild(option);
            }
        }
        
        getSettings();

    } else if (event == Constants.DID_RECEIVE_SETTINGS) {
        applySettings(x['payload']);
    }
}

function sendToPlugin(value, param) {

    if (websocket) {
        const json = {
            "action": actionInfo['action'],
            "event": "sendToPlugin",
            "context": PIuuid,
            "payload": {
                [param] : value
            }
        };

        websocket.send(JSON.stringify(json));
    }
}

function setSettings() {

    appName = document.getElementById("myselect").value;
    keyFunction = (document.getElementById('volUpRadio').checked) ? 'volUp' : 'volDown';

    const o = baseObj;
    o.event = Constants.SET_SETTINGS;
    o.context = PIuuid;
    o.payload[uuid] = {
        'appName': appName,
        'keyFunction': keyFunction
    }

    console.log(o);

    websocket.send(JSON.stringify(o));

    getSettings();
}

function getSettings() {
    let e = {
        'event': Constants.GET_SETTINGS,
        "context": PIuuid
    };

    websocket.send(JSON.stringify(e));
}

function applySettings(payload) {

    console.log("applying settings");

    appName = payload['settings'][uuid]['appName']
    keyFunction = payload['settings'][uuid]['keyFunction']

    document.getElementById("myselect").value = appName;
    var c = (keyFunction == 'volUp') ? document.getElementById("volUpRadio").checked = true : document.getElementById("volDownRadio").checked = true;
}

const baseObj = {
    "event": "",
    "context": "",
    "payload": {}
}
