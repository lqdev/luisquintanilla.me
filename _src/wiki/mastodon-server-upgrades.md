---
post_type: "wiki" 
title: "Mastodon Server Upgrades"
last_updated_date: "02/11/2023 09:30"
tags: selfhost,socialmedia,mastodon,fediverse
---

## Overview

This provides a guide for upgrading specific versions. 

## Backup Instructions

These instructions backup the database and environment variables file. It does not back-up media files.

1.  Create new directory called *backups/\<DATE\>*.
1.  Copy *live/.env.production* to *backups/\<DATE\>* directory.
1.  Dump database `pg_dump -Fc mastodon_production -f /home/mastodon/backups/<DATE>/backup.dump`

## Versions

### 3.4.1

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file.
1. Fetch tags - `git fetch --tags`
1. Checkout 3.4.1 tag - `git checkout v3.4.1`
1. Install Ruby dependencies - `bundle install`
1. Install JS dependencies - `yarn install`
1. Migrate DB - `RAILS_ENV=production bundle exec rails db:migrate`
1. Precompile Assets - `RAILS_ENV=production bundle exec rails assets:precompile`
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.4.2

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file.
1. Fetch tags - `git fetch --tags`
1. Checkout 3.4.2 tag - `git checkout v3.4.2`
1. Install Ruby dependencies - `bundle install`
1. Install JS dependencies - `yarn install`
1. Migrate DB - `RAILS_ENV=production bundle exec rails db:migrate`
1. Precompile Assets - `RAILS_ENV=production bundle exec rails assets:precompile`
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.4.3

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file                                                                           
1. Fetch tags - `git fetch --tags`                                                                        
1. Checkout 3.4.3 tag - `git checkout v3.4.3`                                                             
1. Install Ruby dependencies - `bundle install`                                                                      
1. Install JS dependencies - `yarn install`                                                                          
1. Migrate DB - `RAILS_ENV=production bundle exec rails db:migrate`                                       
1. Precompile Assets - `RAILS_ENV=production bundle exec rails assets:precompile`                         
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.4.4

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file                                                                           
1. Fetch tags - `git fetch --tags`                                                                        
1. Checkout 3.4.4 tag - `git checkout v3.4.4`                                                             
1. Precompile Assets - `RAILS_ENV=production bundle exec rails assets:precompile`
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.4.5

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file                                                                           
1. Fetch tags - `git fetch --tags`                                                                        
1. Checkout 3.4.5 tag - `git checkout v3.4.5`                                                             
1. Install Ruby dependencies - `bundle install`                            
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.4.6

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file   
1. Fetch tags - `git fetch --tags`                                                                        
1. Checkout 3.4.6 tag - `git checkout v3.4.6`                                                             
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.4.7

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file   
1. Fetch tags - `git fetch --tags`                                                                        
1. Checkout 3.4.7 tag - `git checkout v3.4.7`                                                             
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.5.0

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file 
1. Checkout 3.5.0 tag - `git checkout v3.5.0`                                                             
1. Update available rbenv version - `git -C /home/mastodon/.rbenv/plugins/ruby-build pull`                
1. Install Ruby 3.0.3 - `RUBY_CONFIGURE_OPTS=--with-jemalloc rbenv install 3.0.3`                         
1. Install Ruby dependencies - `bundle install`                                                           
1. Install JS dependencies - `yarn install`                                                             
1. Run predeployment DB migration - `SKIP_POST_DEPLOYMENT_MIGRATIONS=true RAILS_ENV=production bundle exec rails db:migrate`
1. Precompile assets - `RAILS_ENV=production bundle exec rails assets:precompile`                         
1. Update service files - `cp /home/mastodon/live/dist/mastodon-*.service /etc/systemd/system/`          
1. Reload systemd daemon - `systemctl daemon-reload`                                                     
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`                 
1. Clear cache - `RAILS_ENV=production bin/tootctl cache clear`                                          
1. Run postdeployment DB migration - `RAILS_ENV=production bundle exec rails db:migrate`                 
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.5.1

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file                                                                           
1. Fetch tags - `git fetch --tags`                                                                        
1. Checkout 3.5.1 tag - `git checkout v3.5.1`                                                             
1. Install Ruby dependencies - `bundle install`                                                           
1. Install JS dependencies - `yarn install`                                                             
1. Precompile assets - `RAILS_ENV=production bundle exec rails assets:precompile`                         
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.5.2

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file                                                                           
1. Fetch tags - `git fetch --tags`                                                                        
1. Checkout 3.5.2 tag - `git checkout v3.5.2`                                                             
1. Install Ruby dependencies - `bundle install`                                                           
1. Install JS dependencies - `yarn install`                                                             
1. Run predeployment DB migration - `SKIP_POST_DEPLOYMENT_MIGRATIONS=true RAILS_ENV=production bundle exec rails db:migrate`
1. Precompile assets - `RAILS_ENV=production bundle exec rails assets:precompile`                         
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`
1. Run postdeployment DB migration - `RAILS_ENV=production bundle exec rails db:migrate`                  
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 3.5.3

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file                                                                           
1. Fetch tags - `git fetch --tags`                                                                        
1. Checkout 3.5.3 tag - `git checkout v3.5.3`                                                             
1. Install Ruby dependencies - `bundle install`                                                           
1. Install JS dependencies - `yarn install`                                                             
1. Precompile assets - `RAILS_ENV=production bundle exec rails assets:precompile`
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.0.0

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file                     
1. Upgade NodeJS - `curl -fsSL https://deb.nodesource.com/setup_16.x | sudo -E bash - && sudo apt-get install -y nodejs`
1. Install Ruby 3.0.4 - `RUBY_CONFIGURE_OPTS=--with-jemalloc rbenv install 3.0.4`
1. Fetch tags - `git fetch --tags`
1. Checkout 4.0.0 tag - `git checkout v4.0.0`
1. Install Ruby dependencies - `bundle install`
1. Install JS dependencies - `yarn install`
1. Run predeployment DB migration - `SKIP_POST_DEPLOYMENT_MIGRATIONS=true RAILS_ENV=production bundle exec rails db:migrate`
1. Precompile assets - `RAILS_ENV=production bundle exec rails assets:precompile`
1. Update service files - `cp /home/mastodon/live/dist/mastodon-*.service /etc/systemd/system/`
1. Reload systemd daemon - `systemctl daemon-reload`
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`
1. Run postdeployment DB migration - `RAILS_ENV=production bundle exec rails db:migrate`
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.0.2

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file
1. Fetch tags - `git fetch --tags`
1. Checkout 4.0.2 tag - `git checkout v4.0.2`
1. Install Ruby dependencies - `bundle install`
1. Precompile assets - `RAILS_ENV=production bundle exec rails assets:precompile`
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

NOTE: Node 18 not supported yet. If you run into issues upgrading directly from 3.5.3, checkout v4.0.0 tag and the upgrade to v4.0.2   

### 4.1.0

1. Stop services - `systemctl stop mastodon-*.service`
1. Backup database and env file
1. Fetch tags - `git fetch --tags`
1. Checkout 4.1.0 tag - `git checkout v4.1.0`
1. Install Ruby dependencies - `bundle install`
1. Install JS dependencies - `yarn install`
1. Run DB migration - `RAILS_ENV=production bundle exec rails db:migrate`
1. Precompile assets - `RAILS_ENV=production bundle exec rails assets:precompile`
1. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.1.1

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.1 tag - `git checkout v4.1.1`
5. Install Ruby dependencies - `bundle install`
6. Install JS dependencies - `yarn install`
7. Precompile assets - `RAILS_ENV=production bundle exec rails assets:precompile`
8. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.1.2

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.3 tag - `git checkout v4.1.2`
5. Update rbenv version - `git -C /home/mastodon/.rbenv/plugins/ruby-build pull`
6. Install Ruby 3.0.6 - `RUBY_CONFIGURE_OPTS=--with-jemalloc rbenv install 3.0.6`
7. Install Ruby dependencies - `bundle install`
8. Install JS dependencies - `yarn install`
9. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.1.3

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.3 tag - `git checkout v4.1.3`
5. Install Ruby dependencies - `bundle install`
6. Install JS dependencies - `yarn install`
7. (Optional) Upgrade reverse proxy `Content-Security-Policy: default-src 'none'; form-action 'none'` & `X-Content-Type-Options: nosniff`. More info can be found in *dist/nginx.conf*.
8. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.1.4

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.4 tag - `git checkout v4.1.4`
5. Install Ruby dependencies - `bundle install`
6. Install JS dependencies - `yarn install`
7. (Optional) Upgrade reverse proxy `Content-Security-Policy: default-src 'none'; form-action 'none'` &`X-Content-Type-Options: nosniff`. More info can be found in *dist/nginx.conf*.
8. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.1.5

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.5 tag - `git checkout v4.1.5`
5. Install Ruby dependencies - `bundle install`
6. Install JS dependencies - `yarn install`
7. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.1.6

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.6 tag - `git checkout v4.1.6`
5. Install Ruby dependencies - `bundle install`
6. Install JS dependencies - `yarn install`
7. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.1.7

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.7 tag - `git checkout v4.1.7`
5. Install Ruby dependencies - `bundle install`
6. Install JS dependencies - `yarn install --frozen-lockfile`
7. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.1.8

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.8 tag - `git checkout v4.1.8`
5. Install Ruby dependencies - `bundle install`
6. Install JS dependencies - `yarn install --frozen-lockfile`
7. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.1.9

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.9 tag - `git checkout v4.1.9`
5. Install Ruby dependencies - `bundle install`
6. Install JS dependencies - `yarn install --frozen-lockfile`
7. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`

### 4.2.0

1. Stop services - `systemctl stop mastodon-*.service`
2. Backup database and env file
3. Fetch tags - `git fetch && git fetch --tags`
4. Checkout 4.1.9 tag - `git checkout v4.2.0`
5. Update Streaming Server Service
    1. `sudo cp ~mastodon/live/dist/mastodon-streaming*.service /etc/systemd/system/`
    1. `sudo systemctl daemon-reload`
6. Update rbenv version - `git -C /home/mastodon/.rbenv/plugins/ruby-build pull`
7. Install Ruby 3.2.2 - `RUBY_CONFIGURE_OPTS=--with-jemalloc rbenv install 3.2.2`
8. Install Ruby dependencies - `bundle install`
9. Install JS dependencies - `yarn install --frozen-lockfile`
10. Precompile assets - `RAILS_ENV=production bundle exec rails assets:precompile`
11. Run predeployment DB migration - `SKIP_POST_DEPLOYMENT_MIGRATIONS=true RAILS_ENV=production bundle exec rails db:migrate`
12. Restart services - `systemctl start mastodon-sidekiq mastodon-web mastodon-streaming`
13. Run postdeployment DB migration - `RAILS_ENV=production bundle exec rails db:migrate`

## Documentation

- https://docs.joinmastodon.org/admin/troubleshooting/index-corruption
- https://docs.joinmastodon.org/admin/install/
- https://docs.joinmastodon.org/admin/upgrading/
- https://docs.joinmastodon.org/admin/backups/
- https://docs.joinmastodon.org/admin/migrating/
- https://docs.joinmastodon.org/admin/tootctl/
