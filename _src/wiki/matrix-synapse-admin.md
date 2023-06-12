---
post_type: "wiki" 
title: "Matrix Synapse Server Administration"
last_updated_date: "06/11/2023 21:13"
tags: messaging,how-to,matrix,self-host,synapse,sql,python
---

## Overview

The following is a guide for managing [Matrix Synapse](https://github.com/matrix-org/synapse) servers.

## Upgrade

1. Initialize Python virtual environment

    ```bash
    source env/bin/activate
    ```

1. Stop server

    ```bash
    synctl stop
    ```

1. Upgrade Synapse package

    ```bash
    pip install --upgrade matrix-synapse[postgres]
    ```

1. Restart server

    ```bash
    synctl restart
    ```

1. Deactivate Python virtual environment

    ```bash
    deactivate
    ```

1. Check version

    ```bash
    curl http://localhost:8008/_synapse/admin/v1/server_version
    ```

## Clean up storage

### Delete old media

1. Run the following command to clean local meadia older than 30 days.

    ```bash
    curl -X POST -H "Authorization: Bearer access_token" http://localhost:8008/_synapse/admin/v1/media/delete?before_ts=$(date +%s000 --date "30 days ago")
    ```

1. Start PSQL

    ```bash
    psql
    ```

1. Connect to database

    ```sql
    \c synapse
    ```

1. Clean up unreferenced itemes

    ```bash
    VACUUM FULL;
    ```

### Delete items in database

1. Start PSQL

    ```bash
    psql
    ```

1. Connect to database

    ```sql
    \c synapse
    ```

1. Run the following SQL script

    ```sql
    DROP FUNCTION IF EXISTS synapse_clean_redacted_messages();
    CREATE FUNCTION synapse_clean_redacted_messages()
        RETURNS void AS $$
        DECLARE
        BEGIN
            UPDATE events SET content = '{}' FROM redactions AS rdc
                WHERE events.event_id = rdc.redacts
                AND (events.type = 'm.room.encrypted' OR events.type = 'm.room.message');
        END;
    $$ LANGUAGE 'plpgsql';

    DROP FUNCTION IF EXISTS synapse_get_server_name();
    CREATE FUNCTION synapse_get_server_name()
        RETURNS text AS $$
        DECLARE
            _someUser TEXT;
            _serverName TEXT;
        BEGIN
            select user_id from account_data limit 1 INTO _someUser;
            select regexp_replace(_someUser, '^.*:', ':') INTO _serverName;
            RETURN _serverName;
        END;
    $$ LANGUAGE 'plpgsql';

    DROP FUNCTION IF EXISTS synapse_get_unused_rooms();
    CREATE FUNCTION synapse_get_unused_rooms()
        RETURNS TABLE(room_id TEXT) AS $$
        DECLARE
        BEGIN
            RETURN QUERY SELECT r.room_id FROM rooms AS r WHERE r.room_id NOT IN (
                SELECT DISTINCT(m.room_id) FROM room_memberships as m
                    INNER JOIN current_state_events as c
                    ON m.event_id = c.event_id
                    AND m.room_id = c.room_id
                    AND m.user_id = c.state_key
                    WHERE c.type = 'm.room.member'
                    AND m.membership = 'join'
                    AND m.user_id LIKE concat('%', synapse_get_server_name())
            );
        END;
    $$ LANGUAGE 'plpgsql';

    DROP FUNCTION IF EXISTS synapse_clean_unused_rooms();
    CREATE FUNCTION synapse_clean_unused_rooms()
        RETURNS void AS $$
        DECLARE
            _count INT;
        BEGIN
            CREATE TEMP TABLE synapse_clean_unused_rooms__tmp
                ON COMMIT DROP
                AS SELECT room_id FROM synapse_get_unused_rooms();

            SELECT COUNT(*) FROM synapse_clean_unused_rooms__tmp INTO _count;
            RAISE NOTICE 'synapse_clean_unused_rooms() Cleaning up % unused rooms', _count;

            DELETE FROM event_forward_extremities AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: event_forward_extremities';

            DELETE FROM event_backward_extremities AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: event_backward_extremities';

            DELETE FROM event_edges AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: event_edges';

            DELETE FROM room_depth AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: room_depth';

            DELETE FROM events AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: events';

            DELETE FROM event_json AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: event_json';

            DELETE FROM state_events AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: state_events';

            DELETE FROM current_state_events AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: current_state_events';

            DELETE FROM room_memberships AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: room_memberships';

            DELETE FROM destination_rooms AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: destination_rooms';

            DELETE FROM event_failed_pull_attempts AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: event_failed_pull_attempts';

            DELETE FROM rooms AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: rooms';

            DELETE FROM room_aliases AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: room_aliases';

            DELETE FROM state_groups AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: state_groups';

            DELETE FROM state_groups_state AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: state_groups_state';

            DELETE FROM receipts_graph AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: receipts_graph';

            DELETE FROM receipts_linearized AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: receipts_linearized';

            DELETE FROM room_tags AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: room_tags';

            DELETE FROM room_tags_revisions AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: room_tags_revisions';

            DELETE FROM room_account_data AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: room_account_data';

            DELETE FROM event_push_actions AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: event_push_actions';

            DELETE FROM pusher_throttle AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: pusher_throttle';

            DELETE FROM event_reports AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: event_reports';

            DELETE FROM stream_ordering_to_exterm AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: stream_ordering_to_exterm';

            DELETE FROM event_auth AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: event_auth';

            DELETE FROM appservice_room_list AS x WHERE x.room_id IN (SELECT y.room_id FROM synapse_clean_unused_rooms__tmp AS y);
            RAISE NOTICE 'DONE: appservice_room_list';
        END;
    $$ LANGUAGE 'plpgsql';

    SELECT synapse_clean_redacted_messages();
    SELECT synapse_clean_unused_rooms();
    ```

1. Clean up unreferenced tuples

    ```sql
    VACUUM FULL;
    ```

## Resources

- [Matrix Synapse Docs](https://matrix-org.github.io/synapse/latest/welcome_and_overview.html)
- [Upgrading Synapse](https://matrix-org.github.io/synapse/latest/upgrade.html)
- [Admin Media API](https://matrix-org.github.io/synapse/v1.38/admin_api/media_admin_api.html)
- [Synapse Scripts](https://github.com/xwiki-labs/synapse_scripts)