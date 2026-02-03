#!/bin/bash
# Start gnome-keyring-daemon if not already running
if ! pgrep -x gnome-keyring-d > /dev/null; then
    eval $(gnome-keyring-daemon --start --components=secrets 2>/dev/null)
    export GNOME_KEYRING_CONTROL
    export SSH_AUTH_SOCK
fi
