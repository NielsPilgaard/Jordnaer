# Chat

## User to User

1. User clicks send message button on other user's profile
1. User is redirected to the /chat page, where a new chat with other user is opened
1. User sends message to backend
1. Backend sends message to Azure Function
   - User is alerted if this fails
1. Azure Function saves message
1. Azure Function sends message to the other user through SignalRHub
1. The other user sees the message

## User to many Users

// TODO