# Task 09: Improve Chat UI

## Context

**App:** Jordnaer (.NET Blazor Server)  
**Area:** Chat/Messaging  
**Priority:** Medium

## Objective

Redesign the chat interface to make it more visually appealing and user-friendly.

## Current State

Chat interface needs visual and UX improvements to feel modern and intuitive.

## Requirements

1. Create modern, clean chat design
2. Clearly distinguish between sender and recipient messages
3. Ensure responsive design for mobile and desktop

## Acceptance Criteria

- [x] Modern chat bubble design implemented
- [x] Clear visual distinction between sent/received messages
- [x] Color scheme is pleasant and accessible
- [x] Fully responsive (mobile & desktop)
- [x] Smooth scrolling and message loading

## Approach Suggestions

1. Review modern chat UI patterns
2. Update chat message component styling
3. Add timestamps and status indicators
4. Implement smooth animations for new messages
5. Test with various message lengths and types

## Files to Investigate

- Chat/messaging components
- `OpenChat.razor`
- Related styling files

NEXT:

1. Add the background styling directly to the MudPaper instead, so we can get a nice gradient: linear-gradient(to bottom, rgba(207, 193, 166, 0.15) 0%, transparent 100%)
2. Scroll-to-bottom is still visible when scrolled down
3. There's a bit of empty space below the chat input on mobile, we don't need that
4. We don't get the topbar back when going from chat -> chat selector on mobile. Find a good solution. If displaying both topbar and header is the solution, that is fine.
5. The individual chat message bubbles are not always the right size. The chat can overflow both to the left and right, and only move to the next line when the user uses enter. The problem is almost certainly in chat.css
