# Remind Me App

A simple website that will allow you to send reminders to yourself and others through e-mail, SMS and/or push notifications.

At the moment this is just a side-project I'm using to learn, but we'll see where it goes.

It isn't production-ready... At all :smile:

## (Planned) Features

**Send reminders as**
- :email: Email
- :iphone: SMS
- :fist_right: Push Notication

**Many reminder scheduling options**

- Send hourly/daily/weekly/monthly/yearly
- Send with a fixed time interval (send reminder every 6 hours for example)
- Send once at a specific date and time.

**Get reminders about upcoming events :calendar:**

- Non-intrusive meeting reminders
    - Log in with your work email and link it's calendar to get meeting reminders when at work, without receiving work-emails on your phone.
    - Configure your work hours to not get reminders when you're off the clock.

**Safety**

- Reminders are only sent to devices that have opted in

## Things I want to gain experience with

- Authentication & Authorization
- Scheduling
- Blazor
- Postgres
- MinimalAPIs
- AWS
    - SES (Simple Email Service)
    - SNS (Simple Notification Service)
    - EC2 (For Web App Hosting)
    - Lambda (For sending reminders)

## Roadmap

- Authentication
    - Reset password
    - MFA
    - Register email
    - Register phone number
- Reminders
    - :email: Email
    - :iphone: SMS
    - :fist_right: Push Notication
    - Scheduling options
    - Calendar sync

## Thanks to

[David Fowler](https://github.com/davidfowl) for making the [TodoApi](https://github.com/davidfowl/TodoApi), a large part of this project is based on that.
