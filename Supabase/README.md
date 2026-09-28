# Supabase Unity Integration

Supabase is the centralized source of truth for stall booking data.

## Confirmed sponsor stall codes

- Main Sponsor: `S 71`
- Expo Sponsor: `S 69`
- Gold Sponsor: `S 68`
- Co-Sponsor: `S 70`

Do not renumber these sponsor stalls.

## Security model

Unity/WebGL is a public client. Use the Supabase **publishable key** (`sb_publishable_...`), never a secret/service-role key.

The SQL migration enables public reads but restricts writes to authenticated Supabase users.

## Setup

1. Run `Supabase/stalls_schema.sql` in the Supabase SQL Editor.
2. In Unity, open `Assets/_Project/Config/AppConfig.asset`.
3. Set:
   - Supabase Project URL
   - Supabase Publishable Key
   - Supabase Stalls Table = `stalls`
4. Keep **Remote Writes Enabled** off until Supabase Auth is connected to the Unity admin login.
5. Seed the `stalls` table using the exact `StallIdentity.StallId` values from the exhibition scene.

At runtime Unity keeps the existing local booking file as a cache/fallback and refreshes booking state from Supabase on startup and at the configured interval.
