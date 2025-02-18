# Chat

## Start Chat Flow:

1. User clicks send message button on other user's profile or group's page
1. User is redirected to the /chat page, where a new chat is opened
1. User sends message to backend
1. System sends message to topic
1. System picks up the message from the queue
1. System saves message
1. System sends message to recipient through SignalRHub
1. Recipient can now see the message if they have the chat open, otherwise they get a notification

---

### Flowchart

```mermaid

flowchart
  UserClick[User clicks send message button on other user's profile or group's page] --> Redirect[User is redirected to /chat]
  Redirect --> MessageBackend[User sends message to backend]
  MessageBackend --> QueueMessage[Backend sends message to queue]
  QueueMessage --> PickMessage[System picks up the message from the queue]
  PickMessage --> SaveMessage[System saves message]
  SaveMessage --> SignalRHub[System sends message to recipient through SignalRHub]
  SignalRHub -- Recipients have the chat open --> MessageVisible[Message becomes visible in chat]
  SignalRHub -- Recipients do not have the chat open --> Notification[Notification is sent to recipients]

```

### Sequence Diagram

```mermaid
sequenceDiagram
  participant U as User
  participant B as Backend
  participant Q as Queue
  participant F as System
  participant S as SignalRHub
  participant R as Recipients

  U->>B: Send message
  B->>Q: Add message to queue
  Q->>F: System picks message
  F->>F: Saves message
  F->>S: Sends message through SignalRHub
  Note over F,S: If recipients have the chat open
  S->>R: Message becomes visible in chat
  Note over F,S: If recipients do not have the chat open
  S->>R: Notification is sent to recipients
```
