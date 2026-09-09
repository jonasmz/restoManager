-- Bootstrap de las dos bases del sistema.
-- POSTGRES_DB (docker-compose) crea 'resto_business'; aquí se añade la de identidad.
SELECT 'CREATE DATABASE resto_identity'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'resto_identity')\gexec
