
var websocket = null;
var PIuuid = null;
var uuid = null;
var actionInfo = {}
var keyFunction = "volUp";
var appName = null;
var coords = null;

// when willAppear fires, plugin will send coords to PI. PI will setSettings with coords of each key and what app they control as well as vol up or down
// Now when key is pressed, plugin can check coords against keys in settings and perform necessary action
// Then plugin will send new values to display to PI

// new: on change, set settings from PI using context and save volfunction and app. plugin will set title on willAppear

function connectElgatoStreamDeckSocket(inPort, inPropertyInspectorUUID, inRegisterEvent, inInfo, inActionInfo) {

    console.log("connecting...");

    PIuuid = inPropertyInspectorUUID;
    actionInfo = JSON.parse(inActionInfo);
    coords = actionInfo['payload']['coordinates']
    websocket = new WebSocket('ws://localhost:' + inPort);
    
    websocket.onopen = function () {
        var json = {
         "event": inRegisterEvent,
         "uuid": inPropertyInspectorUUID
        };
       
        websocket.send(JSON.stringify(json));
        console.log("connected at port: " + inPort);
    };

    websocket.onmessage = function (evt) {                
        processMessage(JSON.parse(evt['data']));

    }

};

function processMessage(msg) {

    let event = msg['event']
    let messageType = msg['payload']['messageType']

    // store UUID of plugin, for use with custom settings
    uuid = msg['context'];  

    console.log(msg);

    // get message from plugin
    if (event == Constants.SEND_TO_PI) {

        if (messageType == "handshake") {
            uuid = msg['context'];
            getSettings();
        }

        if (messageType == "associatedApplication") {
            appName = msg['payload']['appName'];
            console.log(appName);
            document.getElementById("associatedApp").innerText = appName;

            setSettings();
        } else if (messageType == "keyNotFound") {
            // setSettings();
        }

    } else if (event == Constants.DID_RECEIVE_SETTINGS) {
        applySettings(msg['payload']);
    }
}

function sendToPlugin(payload) {

    if (websocket) {
        const json = {
            "action": actionInfo['action'],
            "event": "sendToPlugin",
            "context": PIuuid,
            "payload": {
                payload
            }
        };

        websocket.send(JSON.stringify(json));
    }
}

function getAssociatedApp() {

    // send coordinates and volume function
    // receive application to associate with or null

    const payload = {
        "coordinates": {
            "row": coords.row,
            "column": coords.column
        },
        "keyFunction": keyFunction
    }

    sendToPlugin(payload)
}

function setSettings() {

    keyFunction = (document.getElementById('volUpRadio').checked) ? 'volUp' : 'volDown';

    const o = baseObj;
    o.event = Constants.SET_SETTINGS;
    o.context = PIuuid;
    o.payload[uuid] = {
        'keyFunction': keyFunction,
        'appName': appName
    }

    websocket.send(JSON.stringify(o));
}

function getSettings() {

    console.log("got settings");

    let e = {
        'event': Constants.GET_SETTINGS,
        "context": PIuuid
    };

    websocket.send(JSON.stringify(e));
}

function applySettings(payload) {

    console.log("applying settings");

    // if UUID is undefined, so first time new key is initialized, default it to volUp and setSettings
    // TODO: when volume key is placed, automatically check if it is below or above an application key.
    // Then the key will intelligently map itself to the proper function based on placement. We can also
    // automatically map the associated application to the volume key.
    try {
        keyFunction = payload['settings'][uuid]['keyFunction']
        appName = payload['settings'][uuid]['appName']

        if (appName == null) {
            appName = "No app selected... Please calibrate"
        }

    } catch(err) {
        keyFunction = "volUp";
        appName = "No app selected... Please calibrate"
    }

    document.getElementById("associatedApp").innerText = appName;
    var c = (keyFunction == 'volUp') ? document.getElementById("volUpRadio").checked = true : document.getElementById("volDownRadio").checked = true;
}

function calibrate() {
    getAssociatedApp();
}

const baseObj = {
    "event": "",
    "context": "",
    "payload": {}
}

const coordinates = {
    "row": "",
    "column": ""
}
