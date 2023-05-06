# Chat

## User to User

1. User clicks send message button on other user's profile
1. User is redirected to the /chat page, where a new chat with other user is opened
1. User sends message
1. Message is sent to the backend
1. Backend sends the message to a queue
  - User is alerted if the message delivery fails here
1. A Function picks up the message from the queue
1. The Function saves the message
  - The other user can now see the message if they refresh
1. The message is sent through SignalR Service to the other user
1. The other user sees the message

## User to many Users

// TODO
