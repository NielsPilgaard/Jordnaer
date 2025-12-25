# Task 05: Finish /Posts Feature

## Context

**App:** Jordnaer (.NET Blazor Server)  
**Area:** Posts  
**Priority:** High

## Objective

Complete the implementation of the /Posts page/feature to make it fully functional.

## Current State

The /Posts feature is incomplete and needs additional functionality to be production-ready.

## Requirements

1. Identify missing functionality in /Posts
2. Implement remaining features (e.g., filtering, sorting, pagination)
3. Ensure posts display correctly with sanitized markdown rendering
4. Ensure the create post form looks great
5. Ensure a fitting amount of recent posts (top k) are shown upfront

## Acceptance Criteria

- [ ] All missing functionality identified and documented
- [ ] Filtering implemented (by category, date, author, etc.)
- [ ] Sorting options available (newest, nearby, etc.)
- [ ] Pagination working correctly
- [ ] Markdown rendering displays properly
- [ ] Mobile and desktop layouts work well

## Approach Suggestions

1. First audit the current /Posts implementation
2. List missing features
3. Implement features incrementally

## Files to Investigate

- `/Posts` page component
- Post-related components (likely in `Features/GroupPosts/` or similar)
- `GroupPostList.razor`
