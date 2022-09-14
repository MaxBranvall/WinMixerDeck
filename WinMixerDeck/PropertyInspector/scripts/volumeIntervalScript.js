
var websocket = null;
var PIuuid = null;
var actionInfo = {}
var volInterval = 1;
let mapIterator = Symbol.iterator;

const volIntervals = new Map([
    ['0', 'Mute'],
    ['1', '1'],
    ['2', '2'],
    ['5', '5'],
    ['10', '10'],
    ['25', '25'] 
]);

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
        console.log("connected at port: " + inPort);
    };

    websocket.onmessage = function (evt) {
        processMessage(JSON.parse(evt['data']));
    }

};

function processMessage(msg) {

    console.log(msg);
    let event = msg['event'];
    let messageType = msg['payload']['messageType'];

    uuid = msg['context'];

    if (messageType == "incrementInterval") {
        console.log("inc int");
        changeVolInterval();
    }

    if (messageType == Constants.SEND_TO_PI) {
        uuid = msg['context'];

    }
    
    if (messageType == Constants.HANDSHAKE) {
        populateVolumeIntervals();
        getGlobalSettings();
    }
    
    else if (event == Constants.DID_RECEIVE_GLOBAL_SETTINGS) {
        applyGlobalSettings(msg['payload']);
    }

}

function setGlobalSettings() {

    const ev = baseObj;
    ev.event = Constants.SET_GLOBAL_SETTINGS;
    ev.context = PIuuid;
    ev.payload = {
        "interval": volInterval
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

function applyGlobalSettings(payload) {
    console.log("applying settings");

    volInterval = payload['settings']['interval'];

    document.getElementById("myselect").value = volInterval;
}

function changeVolInterval() {

    let tmp = mapIterator.next().value;
    
    if (tmp == undefined) {
        mapIterator = volIntervals.entries();
        tmp = mapIterator.next().value;
    }
    
    volInterval = tmp[0];

    document.getElementById('myselect').value = volInterval;

    setGlobalSettings();

}

function setVolInterval() {
    volInterval = document.getElementById('myselect').value;
    setGlobalSettings();
}

function populateVolumeIntervals() {

    mapIterator = volIntervals.entries();

    for (const [k, label] of volIntervals.entries()) {
        let option = document.createElement("option");
        option.value = k;
        option.innerHTML = label;

        if (k == '1') {
            option.defaultSelected = true;
        }

        document.getElementById("myselect").appendChild(option);
    }

}

const baseObj = {
    "event": "",
    "context": "",
    "payload": {}
}
