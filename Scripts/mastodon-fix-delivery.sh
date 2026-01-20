#!/bin/bash
# Script to fix Mastodon delivery issues for @lqdev@lqdev.me
# Run this on your Mastodon server

cd /home/mastodon/live

echo "=== Checking Account Status ==="
RAILS_ENV=production bin/rails runner "
account = Account.find_remote('lqdev', 'lqdev.me')
if account
  puts \"Account found: #{account.username}@#{account.domain}\"
  puts \"Suspended: #{account.suspended?}\"
  puts \"Silenced: #{account.silenced?}\"
  puts \"Inbox URL: #{account.inbox_url}\"
  puts \"Last webfingered: #{account.last_webfingered_at}\"
  puts \"Followers: #{account.followers_count}\"
  
  # Check delivery failures
  unavailable = DeliveryFailureTracker.unavailable?(account.inbox_url)
  puts \"Inbox unavailable: #{unavailable}\"
  
  if unavailable
    puts \"\n=== RESETTING DELIVERY FAILURES ===\"
    DeliveryFailureTracker.reset(account.inbox_url)
    puts \"✅ Delivery failures reset for #{account.inbox_url}\"
  end
else
  puts \"❌ Account not found\"
end
"

echo ""
echo "=== Refreshing Account ==="
RAILS_ENV=production bin/tootctl accounts refresh acct:lqdev@lqdev.me

echo ""
echo "=== Checking Sidekiq Failed Jobs ==="
RAILS_ENV=production bin/rails runner "
require 'sidekiq/api'

failed = Sidekiq::DeadSet.new
relevant_jobs = failed.select { |job| 
  job.args.to_s.include?('lqdev.me') || job.item['args'].to_s.include?('lqdev.me')
}

puts \"Total failed jobs: #{failed.size}\"
puts \"Jobs related to lqdev.me: #{relevant_jobs.size}\"

if relevant_jobs.any?
  puts \"\n=== Failed Jobs for lqdev.me ===\"
  relevant_jobs.first(5).each do |job|
    puts \"Class: #{job.klass}\"
    puts \"Failed at: #{job.failed_at}\"
    puts \"Error: #{job.item['error_message']}\"
    puts \"Args: #{job.args}\"
    puts \"---\"
  end
  
  puts \"\n=== RETRYING FAILED JOBS ===\"
  relevant_jobs.each(&:retry)
  puts \"✅ Retried #{relevant_jobs.size} jobs\"
end
"

echo ""
echo "=== Done! Now try following @lqdev@lqdev.me again ==="
