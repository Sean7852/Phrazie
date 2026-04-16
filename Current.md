Need to integrate supabase for registrition/log in/log out
The app split into three services:

- AuthService:
-- Responsibilities:
--- sign up
--- sign in
--- sign out
--- get current session/user
- ProfileService:
-- Responsibilities:
--- create profile row after successful sign-up
--- load current user profile
--- update display name
- SessionStore:
-- Responsibilities:
--- hold the current session/user in memory
--- restore auth state on app startup
--- notify UI when auth state changes

Let's work on the AuthService right now
