# Acceptances tests:

## Active ad dates

- Create Partner
- Partner dashboard
- Partner Details (both as partner and admin)

## email

Visual inspection - Run dotnet test tests/web/Jordnaer.Tests --filter FullyQualifiedName~EmailPreviewTests then open the HTML files in tests/web/Jordnaer.Tests/bin/Debug/email-previews/ in a browser:

confirmation.html - Logo, greeting, yellow CTA button
password-reset.html - Yellow "Nulstil adgangskode" button
group-invite.html - Yellow "Se gruppen" button
chat-notification.html - Yellow "Læs besked" button
delete-user.html - Red "Bekræft sletning" button
group-post-notification.html - Branded blockquote + "Se opslaget" button
membership-request.html - "Se anmodningen" button
partner-welcome.html - Login credentials + "Log ind" button
Layout consistency across all previews:

Logo displays at top
Footer says "Venlig hilsen, Mini Møder Teamet"
600px max-width white card on light gray background
Text color is #41556b (brand blue)
Mobile responsiveness - Resize browser to mobile width, verify emails don't break

Functional testing in dev environment - Trigger actual emails and verify they render in an email client:

Account confirmation email
Chat notification
Group invite
Group post notification
