var clippyLoaded = false;
var characterShown = "";
var displayedAgent;

window.showClippy = (character) => {
    clippy.load(character, function (agent) {

        clippyLoaded = true;
        characterShown = character;

        displayedAgent = agent;

        // do anything with the loaded agent
        displayedAgent.show();
        setTimeout(function () {
            displayedAgent.speak('Hi, I can help with that!');
        }, 1500);

    });
};

window.clippySpeak = (text) => {

    if (clippyLoaded == true) {
        displayedAgent.speak(text);
    }
    else {
        showClippy(characterShown);
    }
};
