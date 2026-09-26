CREATE TABLE IF NOT EXISTS elements (
    id BIGSERIAL PRIMARY KEY,
    attribute_value TEXT NOT NULL,
    html_code TEXT NOT NULL
);
