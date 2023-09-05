window.scrollFunctions = {
    saveScrollPosition: function () {
        if (window.scrollY > 0) {
            sessionStorage.setItem('scrollPosition', window.scrollY);
        }
    },
    loadScrollPosition: function () {
        setTimeout(function () {
            window.scrollTo(0, sessionStorage.getItem('scrollPosition'));
        }, 500);
    },
    scrollToTheBottomOfChat: function () {
        const chatContainer = document.querySelector('.chat-message-window');

        if (!chatContainer) return;

        chatContainer.scrollTop = chatContainer.scrollHeight;
    },
    focusMessageInput: function () {
        const chatMessageInput = document.querySelector('#chat-message-input');

        if (!chatMessageInput) return;

        chatMessageInput.focus();
    }
};
