DO
$$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'app_user') THEN
        EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L', :'app_user', :'app_password');
    ELSE
        EXECUTE format('ALTER ROLE %I WITH LOGIN PASSWORD %L', :'app_user', :'app_password');
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'radius_user') THEN
        EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L', :'radius_user', :'radius_password');
    ELSE
        EXECUTE format('ALTER ROLE %I WITH LOGIN PASSWORD %L', :'radius_user', :'radius_password');
    END IF;
END
$$;
