-- Mera Brand Pakistan - centralized stall database
-- Run this in the Supabase SQL Editor.

create extension if not exists pgcrypto;

create table if not exists public.stalls (
    id uuid primary key default gen_random_uuid(),
    stall_code text not null unique,
    hall text,
    stall_type text not null default 'standard',
    sponsor_type text,
    status text not null default 'available'
        check (status in ('available', 'booked', 'reserved', 'blocked')),
    exhibitor_name text,
    logo_reference text,
    updated_at timestamptz not null default now()
);

alter table public.stalls enable row level security;

grant select on table public.stalls to anon, authenticated;
grant insert, update on table public.stalls to authenticated;

drop policy if exists "Public can read stalls" on public.stalls;
create policy "Public can read stalls"
on public.stalls
for select
to anon, authenticated
using (true);

drop policy if exists "Authenticated users can insert stalls" on public.stalls;
create policy "Authenticated users can insert stalls"
on public.stalls
for insert
to authenticated
with check (true);

drop policy if exists "Authenticated users can update stalls" on public.stalls;
create policy "Authenticated users can update stalls"
on public.stalls
for update
to authenticated
using (true)
with check (true);

-- Sponsor stall numbers confirmed from the current approved floor plan:
-- Main Sponsor = S 71
-- Expo Sponsor = S 69
-- Gold Sponsor = S 68
-- Co-Sponsor   = S 70
