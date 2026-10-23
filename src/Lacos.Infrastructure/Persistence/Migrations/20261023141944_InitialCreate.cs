using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lacos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "patient_public_code_seq");

            migrationBuilder.CreateSequence(
                name: "user_public_code_seq");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    auth0_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    profile_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    profile_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    cpf = table.Column<string>(type: "text", nullable: true),
                    cpf_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    profession = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    ethnicity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    gender = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    anonymized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deduplication_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    birth_place = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ethnicity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cpf = table.Column<string>(type: "text", nullable: true),
                    cpf_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    gender = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    height_cm = table.Column<int>(type: "integer", nullable: false),
                    care_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    temperature_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    glucose_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    weight_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    erased_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patients", x => x.id);
                    table.CheckConstraint("ck_patients_height_cm", "height_cm >= 0 AND height_cm <= 300");
                    table.ForeignKey(
                        name: "fk_patients_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_consents", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_consents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "allergies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    causative_agent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    manifestation = table.Column<string>(type: "text", nullable: false),
                    identified_on = table.Column<DateOnly>(type: "date", nullable: true),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allergies", x => x.id);
                    table.ForeignKey(
                        name: "fk_allergies_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_allergies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_allergies_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_allergies_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "care_diary_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_care_diary_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_care_diary_entries_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_diary_entries_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_diary_entries_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_diary_entries_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "care_routine_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scheduled_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    days_of_week = table.Column<int[]>(type: "integer[]", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_care_routine_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_care_routine_tasks_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_routine_tasks_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_routine_tasks_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_routine_tasks_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "health_conditions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    cid_code = table.Column<string>(type: "text", nullable: false),
                    diagnosis_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_conditions", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_conditions_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_health_conditions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_health_conditions_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_health_conditions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "health_measurements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    measured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    systolic = table.Column<short>(type: "smallint", nullable: true),
                    diastolic = table.Column<short>(type: "smallint", nullable: true),
                    heart_rate = table.Column<short>(type: "smallint", nullable: true),
                    temperature = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    temperature_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    respiratory_rate = table.Column<short>(type: "smallint", nullable: true),
                    oxygen_saturation = table.Column<short>(type: "smallint", nullable: true),
                    blood_glucose = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    glucose_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pain_level = table.Column<short>(type: "smallint", nullable: true),
                    weight = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    weight_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_measurements", x => x.id);
                    table.CheckConstraint("ck_health_measurements_pain", "pain_level IS NULL OR (pain_level BETWEEN 0 AND 10)");
                    table.ForeignKey(
                        name: "fk_health_measurements_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_health_measurements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_health_measurements_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_health_measurements_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "health_thresholds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    systolic_min_limit = table.Column<short>(type: "smallint", nullable: true),
                    systolic_min_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    systolic_max_limit = table.Column<short>(type: "smallint", nullable: true),
                    systolic_max_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    diastolic_min_limit = table.Column<short>(type: "smallint", nullable: true),
                    diastolic_min_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    diastolic_max_limit = table.Column<short>(type: "smallint", nullable: true),
                    diastolic_max_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    heart_bpm_min_limit = table.Column<short>(type: "smallint", nullable: true),
                    heart_bpm_min_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    heart_bpm_max_limit = table.Column<short>(type: "smallint", nullable: true),
                    heart_bpm_max_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    temperature_min_limit = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    temperature_min_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    temperature_max_limit = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    temperature_max_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    respiratory_rate_min_limit = table.Column<short>(type: "smallint", nullable: true),
                    respiratory_rate_min_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    respiratory_rate_max_limit = table.Column<short>(type: "smallint", nullable: true),
                    respiratory_rate_max_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    oxygen_saturation_min_limit = table.Column<short>(type: "smallint", nullable: true),
                    oxygen_saturation_min_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    oxygen_saturation_max_limit = table.Column<short>(type: "smallint", nullable: true),
                    oxygen_saturation_max_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    blood_glucose_min_limit = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    blood_glucose_min_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    blood_glucose_max_limit = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    blood_glucose_max_notice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_thresholds", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_thresholds_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_health_thresholds_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_health_thresholds_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    dosage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    usage_reason = table.Column<string>(type: "text", nullable: true),
                    administration_route = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    prescribed_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    frequency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    administration_times = table.Column<List<TimeOnly>>(type: "time without time zone[]", nullable: false),
                    days_of_week = table.Column<int[]>(type: "integer[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medications", x => x.id);
                    table.ForeignKey(
                        name: "fk_medications_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medications_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medications_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medications_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    medication_reminder = table.Column<bool>(type: "boolean", nullable: false),
                    medication_missed = table.Column<bool>(type: "boolean", nullable: false),
                    appointment_reminder = table.Column<bool>(type: "boolean", nullable: false),
                    care_routine_checkin = table.Column<bool>(type: "boolean", nullable: false),
                    patient_data_added = table.Column<bool>(type: "boolean", nullable: false),
                    health_threshold_alert = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_preferences_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    location_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_events", x => x.id);
                    table.CheckConstraint("ck_patient_events_duration", "duration_minutes IS NULL OR duration_minutes > 0");
                    table.ForeignKey(
                        name: "fk_patient_events_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_events_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_events_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_events_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    relationship_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    can_manage_members = table.Column<bool>(type: "boolean", nullable: false),
                    can_view_patient = table.Column<bool>(type: "boolean", nullable: false),
                    can_add_patient_data = table.Column<bool>(type: "boolean", nullable: false),
                    can_edit_patient_data = table.Column<bool>(type: "boolean", nullable: false),
                    can_delete_patient_data = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_invitations_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_invitations_users_accepted_by_user_id",
                        column: x => x.accepted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_invitations_users_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_user_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    relationship_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    can_manage_members = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_view_patient = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_add_patient_data = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_edit_patient_data = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_delete_patient_data = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_user_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_user_permissions_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "surgeries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedure_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    performed_on = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surgeries", x => x.id);
                    table.ForeignKey(
                        name: "fk_surgeries_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgeries_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgeries_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgeries_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "symptom_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_symptom_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_symptom_records_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_symptom_records_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_symptom_records_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_symptom_records_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "care_routine_completions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_care_routine_completions", x => x.id);
                    table.ForeignKey(
                        name: "fk_care_routine_completions_care_routine_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "care_routine_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_routine_completions_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_routine_completions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_routine_completions_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_routine_completions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medication_administrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    medication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    administered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medication_administrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_medication_administrations_medications_medication_id",
                        column: x => x.medication_id,
                        principalTable: "medications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medication_administrations_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medication_administrations_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medication_administrations_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medication_administrations_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    related_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_documents_patient_events_related_event_id",
                        column: x => x.related_event_id,
                        principalTable: "patient_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_patient_documents_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_documents_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_documents_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_documents_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_allergies_created_by_user_id",
                table: "allergies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_allergies_deleted_by_user_id",
                table: "allergies",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_allergies_patient_id",
                table: "allergies",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_allergies_updated_by_user_id",
                table: "allergies",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_patient_id_created_at",
                table: "audit_logs",
                columns: new[] { "patient_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_diary_entries_created_by_user_id",
                table: "care_diary_entries",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_diary_entries_deleted_by_user_id",
                table: "care_diary_entries",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_diary_entries_patient_id",
                table: "care_diary_entries",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_diary_entries_patient_id_occurred_at",
                table: "care_diary_entries",
                columns: new[] { "patient_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_care_diary_entries_updated_by_user_id",
                table: "care_diary_entries",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_routine_completions_created_by_user_id",
                table: "care_routine_completions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_routine_completions_deleted_by_user_id",
                table: "care_routine_completions",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_routine_completions_patient_id",
                table: "care_routine_completions",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_routine_completions_task_id_scheduled_date",
                table: "care_routine_completions",
                columns: new[] { "task_id", "scheduled_date" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_care_routine_completions_updated_by_user_id",
                table: "care_routine_completions",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_routine_tasks_created_by_user_id",
                table: "care_routine_tasks",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_routine_tasks_deleted_by_user_id",
                table: "care_routine_tasks",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_routine_tasks_patient_id",
                table: "care_routine_tasks",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_care_routine_tasks_updated_by_user_id",
                table: "care_routine_tasks",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_conditions_created_by_user_id",
                table: "health_conditions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_conditions_deleted_by_user_id",
                table: "health_conditions",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_conditions_patient_id",
                table: "health_conditions",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_conditions_updated_by_user_id",
                table: "health_conditions",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_measurements_created_by_user_id",
                table: "health_measurements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_measurements_deleted_by_user_id",
                table: "health_measurements",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_measurements_patient_id",
                table: "health_measurements",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_measurements_patient_id_measured_at",
                table: "health_measurements",
                columns: new[] { "patient_id", "measured_at" });

            migrationBuilder.CreateIndex(
                name: "ix_health_measurements_updated_by_user_id",
                table: "health_measurements",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_thresholds_created_by_user_id",
                table: "health_thresholds",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_thresholds_patient_id",
                table: "health_thresholds",
                column: "patient_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_health_thresholds_updated_by_user_id",
                table: "health_thresholds",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_created_by_user_id",
                table: "medication_administrations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_deleted_by_user_id",
                table: "medication_administrations",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_medication_id_scheduled_for",
                table: "medication_administrations",
                columns: new[] { "medication_id", "scheduled_for" },
                unique: true,
                filter: "scheduled_for IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_patient_id",
                table: "medication_administrations",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_patient_id_administered_at",
                table: "medication_administrations",
                columns: new[] { "patient_id", "administered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_updated_by_user_id",
                table: "medication_administrations",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_medications_created_by_user_id",
                table: "medications",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_medications_deleted_by_user_id",
                table: "medications",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_medications_patient_id",
                table: "medications",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_medications_patient_id_start_date_end_date",
                table: "medications",
                columns: new[] { "patient_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_medications_updated_by_user_id",
                table: "medications",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_patient_id_user_id",
                table: "notification_preferences",
                columns: new[] { "patient_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_user_id",
                table: "notification_preferences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_deduplication_key",
                table: "notifications",
                column: "deduplication_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_status_created_at",
                table: "notifications",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_read_at",
                table: "notifications",
                columns: new[] { "user_id", "read_at" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_documents_created_by_user_id",
                table: "patient_documents",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_documents_deleted_by_user_id",
                table: "patient_documents",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_documents_patient_id",
                table: "patient_documents",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_documents_related_event_id",
                table: "patient_documents",
                column: "related_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_documents_updated_by_user_id",
                table: "patient_documents",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_events_created_by_user_id",
                table: "patient_events",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_events_deleted_by_user_id",
                table: "patient_events",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_events_patient_id",
                table: "patient_events",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_events_patient_id_scheduled_at",
                table: "patient_events",
                columns: new[] { "patient_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_events_status_scheduled_at",
                table: "patient_events",
                columns: new[] { "status", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_events_updated_by_user_id",
                table: "patient_events",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_invitations_accepted_by_user_id",
                table: "patient_invitations",
                column: "accepted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_invitations_invited_by_user_id",
                table: "patient_invitations",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_invitations_invited_email_status",
                table: "patient_invitations",
                columns: new[] { "invited_email", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_invitations_patient_id_status",
                table: "patient_invitations",
                columns: new[] { "patient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_user_permissions_patient_id_user_id",
                table: "patient_user_permissions",
                columns: new[] { "patient_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_user_permissions_user_id_is_active",
                table: "patient_user_permissions",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_patients_cpf_hash",
                table: "patients",
                column: "cpf_hash",
                unique: true,
                filter: "cpf_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_patients_created_by_user_id",
                table: "patients",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_full_name",
                table: "patients",
                column: "full_name");

            migrationBuilder.CreateIndex(
                name: "ix_patients_public_code",
                table: "patients",
                column: "public_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_surgeries_created_by_user_id",
                table: "surgeries",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgeries_deleted_by_user_id",
                table: "surgeries",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgeries_patient_id",
                table: "surgeries",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgeries_updated_by_user_id",
                table: "surgeries",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_symptom_records_created_by_user_id",
                table: "symptom_records",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_symptom_records_deleted_by_user_id",
                table: "symptom_records",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_symptom_records_patient_id",
                table: "symptom_records",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_symptom_records_patient_id_occurred_at",
                table: "symptom_records",
                columns: new[] { "patient_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_symptom_records_updated_by_user_id",
                table: "symptom_records",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_consents_user_id_consent_type",
                table: "user_consents",
                columns: new[] { "user_id", "consent_type" });

            migrationBuilder.CreateIndex(
                name: "ix_users_auth0_user_id",
                table: "users",
                column: "auth0_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_cpf_hash",
                table: "users",
                column: "cpf_hash",
                unique: true,
                filter: "cpf_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_public_code",
                table: "users",
                column: "public_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allergies");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "care_diary_entries");

            migrationBuilder.DropTable(
                name: "care_routine_completions");

            migrationBuilder.DropTable(
                name: "health_conditions");

            migrationBuilder.DropTable(
                name: "health_measurements");

            migrationBuilder.DropTable(
                name: "health_thresholds");

            migrationBuilder.DropTable(
                name: "medication_administrations");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "patient_documents");

            migrationBuilder.DropTable(
                name: "patient_invitations");

            migrationBuilder.DropTable(
                name: "patient_user_permissions");

            migrationBuilder.DropTable(
                name: "surgeries");

            migrationBuilder.DropTable(
                name: "symptom_records");

            migrationBuilder.DropTable(
                name: "user_consents");

            migrationBuilder.DropTable(
                name: "care_routine_tasks");

            migrationBuilder.DropTable(
                name: "medications");

            migrationBuilder.DropTable(
                name: "patient_events");

            migrationBuilder.DropTable(
                name: "patients");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropSequence(
                name: "patient_public_code_seq");

            migrationBuilder.DropSequence(
                name: "user_public_code_seq");
        }
    }
}
