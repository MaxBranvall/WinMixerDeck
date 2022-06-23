
import { Constants } from "./constants";

var websocket = null;
var PIuuid = null;
var actionInfo = {}
var volInterval = null;

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
        getGlobalSettings();
    };

    websocket.onmessage = function (evt) {
        var msg = JSON.parse(evt['data'])
        processMessage(msg);
    }

};

function processMessage(msg) {

    let event = msg['event']

    if (event == Constants.DID_RECEIVE_GLOBAL_SETTINGS) {
        volInterval = x['payload']['settings']['volInterval'];
        document.getElementById("myselect").value = volInterval;
    }

}

function setGlobalSettings() {
    volInterval = document.getElementById("myselect").value;

    const ev = baseObj;
    ev.event = Constants.SET_GLOBAL_SETTINGS;
    ev.context = PIuuid;
    ev.payload = {
        "volInterval": volInterval
    }

    websocket.send(JSON.stringify(ev));
}

function getGlobalSettings() {
    const json = {
        "event": Constants.GET_GLOBAL_SETTINGS,
        "context": PIuuid
    }

    websocket.send(JSON.stringify(json));

}

const baseObj = {
    "event": "",
    "context": "",
    "payload": {}
}
