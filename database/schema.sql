CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE SEQUENCE patient_public_code_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE SEQUENCE user_public_code_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        public_code character varying(12) NOT NULL,
        auth0_user_id character varying(255) NOT NULL,
        name character varying(150) NOT NULL,
        email character varying(255) NOT NULL,
        phone character varying(30),
        profile_type character varying(30),
        profile_completed boolean NOT NULL DEFAULT FALSE,
        is_active boolean NOT NULL DEFAULT TRUE,
        cpf text,
        cpf_hash character varying(64),
        birth_date date,
        profession character varying(100),
        cep character varying(8),
        ethnicity character varying(50),
        gender character varying(50),
        anonymized_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_users PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE notifications (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        patient_id uuid,
        category character varying(50) NOT NULL,
        title character varying(150) NOT NULL,
        message character varying(1000) NOT NULL,
        related_entity_id uuid,
        deduplication_key character varying(200) NOT NULL,
        status character varying(50) NOT NULL,
        attempts integer NOT NULL,
        last_error character varying(1000),
        sent_at timestamp with time zone,
        read_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_notifications PRIMARY KEY (id),
        CONSTRAINT fk_notifications_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE patients (
        id uuid NOT NULL,
        public_code character varying(12) NOT NULL,
        created_by_user_id uuid NOT NULL,
        full_name character varying(150) NOT NULL,
        birth_date date NOT NULL,
        birth_place character varying(150) NOT NULL,
        cep character varying(8) NOT NULL,
        ethnicity character varying(50) NOT NULL,
        cpf text,
        cpf_hash character varying(64),
        gender character varying(50) NOT NULL,
        email character varying(255),
        phone character varying(30),
        height_cm integer NOT NULL,
        care_status character varying(50) NOT NULL,
        temperature_unit character varying(50) NOT NULL,
        glucose_unit character varying(50) NOT NULL,
        weight_unit character varying(50) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        erased_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_patients PRIMARY KEY (id),
        CONSTRAINT ck_patients_height_cm CHECK (height_cm >= 0 AND height_cm <= 300),
        CONSTRAINT fk_patients_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE user_consents (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        consent_type character varying(50) NOT NULL,
        version character varying(20) NOT NULL,
        accepted_at timestamp with time zone NOT NULL,
        ip_address character varying(45),
        revoked_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_user_consents PRIMARY KEY (id),
        CONSTRAINT fk_user_consents_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE allergies (
        id uuid NOT NULL,
        causative_agent character varying(200) NOT NULL,
        manifestation text NOT NULL,
        identified_on date,
        severity character varying(50) NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_allergies PRIMARY KEY (id),
        CONSTRAINT fk_allergies_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_allergies_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_allergies_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_allergies_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE audit_logs (
        id uuid NOT NULL,
        user_id uuid,
        patient_id uuid,
        entity_type character varying(100) NOT NULL,
        entity_id uuid,
        action character varying(30) NOT NULL,
        old_values jsonb,
        new_values jsonb,
        reason text,
        ip_address character varying(45),
        user_agent character varying(512),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_audit_logs PRIMARY KEY (id),
        CONSTRAINT fk_audit_logs_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_audit_logs_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE care_diary_entries (
        id uuid NOT NULL,
        category character varying(50) NOT NULL,
        description text NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_care_diary_entries PRIMARY KEY (id),
        CONSTRAINT fk_care_diary_entries_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_diary_entries_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_diary_entries_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_diary_entries_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE care_routine_tasks (
        id uuid NOT NULL,
        title character varying(150) NOT NULL,
        description character varying(1000),
        category character varying(50) NOT NULL,
        scheduled_time time without time zone NOT NULL,
        days_of_week integer[] NOT NULL,
        start_date date NOT NULL,
        end_date date,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_care_routine_tasks PRIMARY KEY (id),
        CONSTRAINT fk_care_routine_tasks_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_routine_tasks_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_routine_tasks_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_routine_tasks_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE health_conditions (
        id uuid NOT NULL,
        name text NOT NULL,
        cid_code text NOT NULL,
        diagnosis_date date,
        status character varying(50) NOT NULL,
        notes text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_health_conditions PRIMARY KEY (id),
        CONSTRAINT fk_health_conditions_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_health_conditions_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_health_conditions_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_health_conditions_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE health_measurements (
        id uuid NOT NULL,
        measured_at timestamp with time zone NOT NULL,
        systolic smallint,
        diastolic smallint,
        heart_rate smallint,
        temperature numeric(4,1),
        temperature_unit character varying(50) NOT NULL,
        respiratory_rate smallint,
        oxygen_saturation smallint,
        blood_glucose numeric(6,2),
        glucose_unit character varying(50) NOT NULL,
        pain_level smallint,
        weight numeric(6,2),
        weight_unit character varying(50) NOT NULL,
        description text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_health_measurements PRIMARY KEY (id),
        CONSTRAINT ck_health_measurements_pain CHECK (pain_level IS NULL OR (pain_level BETWEEN 0 AND 10)),
        CONSTRAINT fk_health_measurements_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_health_measurements_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_health_measurements_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_health_measurements_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE health_thresholds (
        id uuid NOT NULL,
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        systolic_min_limit smallint,
        systolic_min_notice boolean NOT NULL DEFAULT FALSE,
        systolic_max_limit smallint,
        systolic_max_notice boolean NOT NULL DEFAULT FALSE,
        diastolic_min_limit smallint,
        diastolic_min_notice boolean NOT NULL DEFAULT FALSE,
        diastolic_max_limit smallint,
        diastolic_max_notice boolean NOT NULL DEFAULT FALSE,
        heart_bpm_min_limit smallint,
        heart_bpm_min_notice boolean NOT NULL DEFAULT FALSE,
        heart_bpm_max_limit smallint,
        heart_bpm_max_notice boolean NOT NULL DEFAULT FALSE,
        temperature_min_limit numeric(4,1),
        temperature_min_notice boolean NOT NULL DEFAULT FALSE,
        temperature_max_limit numeric(4,1),
        temperature_max_notice boolean NOT NULL DEFAULT FALSE,
        respiratory_rate_min_limit smallint,
        respiratory_rate_min_notice boolean NOT NULL DEFAULT FALSE,
        respiratory_rate_max_limit smallint,
        respiratory_rate_max_notice boolean NOT NULL DEFAULT FALSE,
        oxygen_saturation_min_limit smallint,
        oxygen_saturation_min_notice boolean NOT NULL DEFAULT FALSE,
        oxygen_saturation_max_limit smallint,
        oxygen_saturation_max_notice boolean NOT NULL DEFAULT FALSE,
        blood_glucose_min_limit numeric(6,2),
        blood_glucose_min_notice boolean NOT NULL DEFAULT FALSE,
        blood_glucose_max_limit numeric(6,2),
        blood_glucose_max_notice boolean NOT NULL DEFAULT FALSE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_health_thresholds PRIMARY KEY (id),
        CONSTRAINT fk_health_thresholds_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_health_thresholds_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_health_thresholds_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE medications (
        id uuid NOT NULL,
        name character varying(150) NOT NULL,
        dosage character varying(100) NOT NULL,
        brand character varying(100),
        usage_reason text,
        administration_route character varying(50) NOT NULL,
        prescribed_by character varying(150),
        start_date date NOT NULL,
        end_date date,
        frequency character varying(100) NOT NULL,
        administration_times time without time zone[] NOT NULL,
        days_of_week integer[] NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_medications PRIMARY KEY (id),
        CONSTRAINT fk_medications_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_medications_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_medications_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_medications_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE notification_preferences (
        id uuid NOT NULL,
        patient_id uuid NOT NULL,
        user_id uuid NOT NULL,
        medication_reminder boolean NOT NULL,
        medication_missed boolean NOT NULL,
        appointment_reminder boolean NOT NULL,
        care_routine_checkin boolean NOT NULL,
        patient_data_added boolean NOT NULL,
        health_threshold_alert boolean NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_notification_preferences PRIMARY KEY (id),
        CONSTRAINT fk_notification_preferences_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_notification_preferences_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE patient_events (
        id uuid NOT NULL,
        event_type character varying(30) NOT NULL,
        title character varying(150) NOT NULL,
        scheduled_at timestamp with time zone NOT NULL,
        duration_minutes integer,
        location_name character varying(200),
        provider_name character varying(150),
        status character varying(30) NOT NULL,
        summary text,
        notes text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_patient_events PRIMARY KEY (id),
        CONSTRAINT ck_patient_events_duration CHECK (duration_minutes IS NULL OR duration_minutes > 0),
        CONSTRAINT fk_patient_events_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_patient_events_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_patient_events_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_patient_events_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE patient_invitations (
        id uuid NOT NULL,
        patient_id uuid NOT NULL,
        invited_email character varying(255) NOT NULL,
        invited_by_user_id uuid NOT NULL,
        role character varying(50) NOT NULL,
        relationship_type character varying(50) NOT NULL,
        can_manage_members boolean NOT NULL,
        can_view_patient boolean NOT NULL,
        can_add_patient_data boolean NOT NULL,
        can_edit_patient_data boolean NOT NULL,
        can_delete_patient_data boolean NOT NULL,
        status character varying(50) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        responded_at timestamp with time zone,
        accepted_by_user_id uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_patient_invitations PRIMARY KEY (id),
        CONSTRAINT fk_patient_invitations_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_patient_invitations_users_accepted_by_user_id FOREIGN KEY (accepted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_patient_invitations_users_invited_by_user_id FOREIGN KEY (invited_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE patient_user_permissions (
        id uuid NOT NULL,
        patient_id uuid NOT NULL,
        user_id uuid NOT NULL,
        role character varying(50) NOT NULL,
        relationship_type character varying(50) NOT NULL,
        can_manage_members boolean NOT NULL DEFAULT FALSE,
        can_view_patient boolean NOT NULL DEFAULT FALSE,
        can_add_patient_data boolean NOT NULL DEFAULT FALSE,
        can_edit_patient_data boolean NOT NULL DEFAULT FALSE,
        can_delete_patient_data boolean NOT NULL DEFAULT FALSE,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_patient_user_permissions PRIMARY KEY (id),
        CONSTRAINT fk_patient_user_permissions_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_patient_user_permissions_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE surgeries (
        id uuid NOT NULL,
        procedure_name character varying(200) NOT NULL,
        performed_on date NOT NULL,
        reason text NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_surgeries PRIMARY KEY (id),
        CONSTRAINT fk_surgeries_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_surgeries_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_surgeries_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_surgeries_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE symptom_records (
        id uuid NOT NULL,
        type character varying(50) NOT NULL,
        description text NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_symptom_records PRIMARY KEY (id),
        CONSTRAINT fk_symptom_records_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_symptom_records_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_symptom_records_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_symptom_records_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE care_routine_completions (
        id uuid NOT NULL,
        task_id uuid NOT NULL,
        scheduled_date date NOT NULL,
        completed_at timestamp with time zone NOT NULL,
        notes text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_care_routine_completions PRIMARY KEY (id),
        CONSTRAINT fk_care_routine_completions_care_routine_tasks_task_id FOREIGN KEY (task_id) REFERENCES care_routine_tasks (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_routine_completions_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_routine_completions_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_routine_completions_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_care_routine_completions_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE medication_administrations (
        id uuid NOT NULL,
        medication_id uuid NOT NULL,
        scheduled_for timestamp with time zone,
        administered_at timestamp with time zone NOT NULL,
        status character varying(50) NOT NULL,
        notes text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_medication_administrations PRIMARY KEY (id),
        CONSTRAINT fk_medication_administrations_medications_medication_id FOREIGN KEY (medication_id) REFERENCES medications (id) ON DELETE RESTRICT,
        CONSTRAINT fk_medication_administrations_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_medication_administrations_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_medication_administrations_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_medication_administrations_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE TABLE patient_documents (
        id uuid NOT NULL,
        file_name character varying(200) NOT NULL,
        content_type character varying(100) NOT NULL,
        size_bytes bigint NOT NULL,
        sha256 character varying(64) NOT NULL,
        storage_path character varying(300) NOT NULL,
        category character varying(50) NOT NULL,
        description text,
        related_event_id uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        patient_id uuid NOT NULL,
        created_by_user_id uuid NOT NULL,
        updated_by_user_id uuid,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        CONSTRAINT pk_patient_documents PRIMARY KEY (id),
        CONSTRAINT fk_patient_documents_patient_events_related_event_id FOREIGN KEY (related_event_id) REFERENCES patient_events (id) ON DELETE SET NULL,
        CONSTRAINT fk_patient_documents_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_patient_documents_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_patient_documents_users_deleted_by_user_id FOREIGN KEY (deleted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT fk_patient_documents_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_allergies_created_by_user_id ON allergies (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_allergies_deleted_by_user_id ON allergies (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_allergies_patient_id ON allergies (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_allergies_updated_by_user_id ON allergies (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_audit_logs_entity_type_entity_id ON audit_logs (entity_type, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_audit_logs_patient_id_created_at ON audit_logs (patient_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_audit_logs_user_id ON audit_logs (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_diary_entries_created_by_user_id ON care_diary_entries (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_diary_entries_deleted_by_user_id ON care_diary_entries (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_diary_entries_patient_id ON care_diary_entries (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_diary_entries_patient_id_occurred_at ON care_diary_entries (patient_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_diary_entries_updated_by_user_id ON care_diary_entries (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_routine_completions_created_by_user_id ON care_routine_completions (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_routine_completions_deleted_by_user_id ON care_routine_completions (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_routine_completions_patient_id ON care_routine_completions (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_care_routine_completions_task_id_scheduled_date ON care_routine_completions (task_id, scheduled_date) WHERE deleted_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_routine_completions_updated_by_user_id ON care_routine_completions (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_routine_tasks_created_by_user_id ON care_routine_tasks (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_routine_tasks_deleted_by_user_id ON care_routine_tasks (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_routine_tasks_patient_id ON care_routine_tasks (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_care_routine_tasks_updated_by_user_id ON care_routine_tasks (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_conditions_created_by_user_id ON health_conditions (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_conditions_deleted_by_user_id ON health_conditions (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_conditions_patient_id ON health_conditions (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_conditions_updated_by_user_id ON health_conditions (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_measurements_created_by_user_id ON health_measurements (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_measurements_deleted_by_user_id ON health_measurements (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_measurements_patient_id ON health_measurements (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_measurements_patient_id_measured_at ON health_measurements (patient_id, measured_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_measurements_updated_by_user_id ON health_measurements (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_thresholds_created_by_user_id ON health_thresholds (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_health_thresholds_patient_id ON health_thresholds (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_health_thresholds_updated_by_user_id ON health_thresholds (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medication_administrations_created_by_user_id ON medication_administrations (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medication_administrations_deleted_by_user_id ON medication_administrations (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_medication_administrations_medication_id_scheduled_for ON medication_administrations (medication_id, scheduled_for) WHERE scheduled_for IS NOT NULL AND deleted_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medication_administrations_patient_id ON medication_administrations (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medication_administrations_patient_id_administered_at ON medication_administrations (patient_id, administered_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medication_administrations_updated_by_user_id ON medication_administrations (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medications_created_by_user_id ON medications (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medications_deleted_by_user_id ON medications (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medications_patient_id ON medications (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medications_patient_id_start_date_end_date ON medications (patient_id, start_date, end_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_medications_updated_by_user_id ON medications (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_notification_preferences_patient_id_user_id ON notification_preferences (patient_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_notification_preferences_user_id ON notification_preferences (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_notifications_deduplication_key ON notifications (deduplication_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_notifications_status_created_at ON notifications (status, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_notifications_user_id_read_at ON notifications (user_id, read_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_documents_created_by_user_id ON patient_documents (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_documents_deleted_by_user_id ON patient_documents (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_documents_patient_id ON patient_documents (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_documents_related_event_id ON patient_documents (related_event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_documents_updated_by_user_id ON patient_documents (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_events_created_by_user_id ON patient_events (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_events_deleted_by_user_id ON patient_events (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_events_patient_id ON patient_events (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_events_patient_id_scheduled_at ON patient_events (patient_id, scheduled_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_events_status_scheduled_at ON patient_events (status, scheduled_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_events_updated_by_user_id ON patient_events (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_invitations_accepted_by_user_id ON patient_invitations (accepted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_invitations_invited_by_user_id ON patient_invitations (invited_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_invitations_invited_email_status ON patient_invitations (invited_email, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_invitations_patient_id_status ON patient_invitations (patient_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_patient_user_permissions_patient_id_user_id ON patient_user_permissions (patient_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patient_user_permissions_user_id_is_active ON patient_user_permissions (user_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_patients_cpf_hash ON patients (cpf_hash) WHERE cpf_hash IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patients_created_by_user_id ON patients (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_patients_full_name ON patients (full_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_patients_public_code ON patients (public_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_surgeries_created_by_user_id ON surgeries (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_surgeries_deleted_by_user_id ON surgeries (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_surgeries_patient_id ON surgeries (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_surgeries_updated_by_user_id ON surgeries (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_symptom_records_created_by_user_id ON symptom_records (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_symptom_records_deleted_by_user_id ON symptom_records (deleted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_symptom_records_patient_id ON symptom_records (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_symptom_records_patient_id_occurred_at ON symptom_records (patient_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_symptom_records_updated_by_user_id ON symptom_records (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE INDEX ix_user_consents_user_id_consent_type ON user_consents (user_id, consent_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_auth0_user_id ON users (auth0_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_cpf_hash ON users (cpf_hash) WHERE cpf_hash IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_email ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_public_code ON users (public_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141944_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261023141944_InitialCreate', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    CREATE OR REPLACE FUNCTION audit_logs_immutable() RETURNS trigger AS $$
    BEGIN
        IF TG_OP = 'DELETE' THEN
            RAISE EXCEPTION 'audit_logs é imutável: exclusão não permitida';
        END IF;
        IF current_setting('lacos.allow_audit_redaction', true) = 'on'
           AND NEW.old_values IS NULL AND NEW.new_values IS NULL
           AND NEW.id = OLD.id AND NEW.user_id IS NOT DISTINCT FROM OLD.user_id
           AND NEW.patient_id IS NOT DISTINCT FROM OLD.patient_id AND NEW.entity_type = OLD.entity_type
           AND NEW.entity_id IS NOT DISTINCT FROM OLD.entity_id AND NEW.action = OLD.action
           AND NEW.created_at = OLD.created_at THEN
            RETURN NEW;
        END IF;
        RAISE EXCEPTION 'audit_logs é imutável: alteração não permitida';
    END;
    $$ LANGUAGE plpgsql;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    CREATE TRIGGER trg_audit_logs_immutable
    BEFORE UPDATE OR DELETE ON audit_logs
    FOR EACH ROW EXECUTE FUNCTION audit_logs_immutable();
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    CREATE OR REPLACE FUNCTION redact_patient_audit_values(p_patient_id uuid) RETURNS integer AS $$
    DECLARE affected integer;
    BEGIN
        PERFORM set_config('lacos.allow_audit_redaction', 'on', true);
        UPDATE audit_logs SET old_values = NULL, new_values = NULL
         WHERE patient_id = p_patient_id AND (old_values IS NOT NULL OR new_values IS NOT NULL);
        GET DIAGNOSTICS affected = ROW_COUNT;
        PERFORM set_config('lacos.allow_audit_redaction', 'off', true);
        RETURN affected;
    END;
    $$ LANGUAGE plpgsql;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE users ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE user_consents ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE patients ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE patient_user_permissions ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE patient_invitations ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE health_thresholds ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE patient_events ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE notification_preferences ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE medications ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE medication_administrations ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE health_measurements ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE symptom_records ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE care_diary_entries ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE care_routine_tasks ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE care_routine_completions ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE health_conditions ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE surgeries ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE allergies ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    ALTER TABLE patient_documents ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    DO $$
    BEGIN
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
            EXECUTE 'REVOKE ALL ON users, user_consents, patients, patient_user_permissions, patient_invitations, health_thresholds, patient_events, notification_preferences, notifications, audit_logs, medications, medication_administrations, health_measurements, symptom_records, care_diary_entries, care_routine_tasks, care_routine_completions, health_conditions, surgeries, allergies, patient_documents FROM anon';
            EXECUTE 'REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM anon';
        END IF;
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
            EXECUTE 'REVOKE ALL ON users, user_consents, patients, patient_user_permissions, patient_invitations, health_thresholds, patient_events, notification_preferences, notifications, audit_logs, medications, medication_administrations, health_measurements, symptom_records, care_diary_entries, care_routine_tasks, care_routine_completions, health_conditions, surgeries, allergies, patient_documents FROM authenticated';
            EXECUTE 'REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM authenticated';
        END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261023141958_DatabaseSecurity') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261023141958_DatabaseSecurity', '8.0.8');
    END IF;
END $EF$;
COMMIT;

