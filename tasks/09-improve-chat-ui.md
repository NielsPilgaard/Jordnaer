# Task 09: Improve Chat UI

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Chat/Messaging
**Priority:** Medium

## Objective

Redesign the chat interface to make it more visually appealing and user-friendly with warm, cohesive colors and better UX.

## Current State

Chat interface needs visual and UX improvements to feel modern and intuitive with our warm color palette.

## Requirements

### For Mobile:

- Use new-style back button and a warm color (tan-ish) for the chat header
- It should be possible to see when a message was sent. If using a tooltip, the tooltip should not be rendered outside the view, causing a scroll-x effect

### For Both Mobile and Desktop:

- Update the send button with a more common "send" button icon, and make the color warmer
- The text input highlights purple when selected, it should be a green-ish tint instead
- The search for user input is dull and doesn't match our new UI quality. Improve it.
- The ScrollToBottom should scroll smoothly

### For Desktop:

- When a chat is selected, it should have a warm green-ish tint on the side
- The entire MudGrid/MudPaper should have this background: `linear-gradient(to bottom, rgba(207, 193, 166, 0.15) 0%, transparent 100%)`

## Acceptance Criteria

- [ ] Mobile chat header uses warm tan-ish color and new-style back button
- [ ] Message timestamps visible without causing horizontal scroll on mobile
- [ ] Send button uses common send icon with warm color
- [ ] Text input focus color is green-ish tint (not purple)
- [ ] User search input matches new UI quality
- [ ] ScrollToBottom has smooth scrolling behavior
- [ ] Selected chat on desktop has warm green-ish tint on side
- [ ] Desktop chat container has gradient background as specified
- [ ] Fully responsive (mobile & desktop)

## Files to Investigate

- `src/web/Jordnaer/Pages/Chat/ChatPage.razor`
- `src/web/Jordnaer/Features/Chat/ChatMessageList.razor`
- `src/web/Jordnaer/Components/ScrollToBottom.razor.cs`
- `src/web/Jordnaer/wwwroot/css/chat.css`
