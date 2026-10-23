using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lacos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Regras de segurança aplicadas diretamente no banco (defesa em profundidade):
    ///
    /// 1. Trilha de auditoria IMUTÁVEL (RNF04): trigger bloqueia DELETE e UPDATE em audit_logs.
    ///    Única exceção: a função redact_patient_audit_values, usada na exclusão definitiva (LGPD - RNF01),
    ///    que apenas anula old_values/new_values, preservando quem fez o quê e quando.
    /// 2. Row Level Security (RLS) habilitado em todas as tabelas e privilégios revogados dos papéis
    ///    "anon" e "authenticated" do Supabase: os dados ficam inacessíveis pela API REST pública do Supabase
    ///    (PostgREST); somente este back-end (conexão com o usuário do banco) acessa as tabelas.
    /// </summary>
    public partial class DatabaseSecurity : Migration
    {
        private static readonly string[] Tables =
        {
            "users", "user_consents", "patients", "patient_user_permissions", "patient_invitations", "health_thresholds",
            "patient_events", "notification_preferences", "notifications", "audit_logs", "medications", "medication_administrations",
            "health_measurements", "symptom_records", "care_diary_entries", "care_routine_tasks", "care_routine_completions",
            "health_conditions", "surgeries", "allergies", "patient_documents"
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
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
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_audit_logs_immutable
                BEFORE UPDATE OR DELETE ON audit_logs
                FOR EACH ROW EXECUTE FUNCTION audit_logs_immutable();
                """);

            migrationBuilder.Sql("""
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
                """);

            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.Sql($"""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                        EXECUTE 'REVOKE ALL ON {string.Join(", ", Tables)} FROM anon';
                        EXECUTE 'REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM anon';
                    END IF;
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                        EXECUTE 'REVOKE ALL ON {string.Join(", ", Tables)} FROM authenticated';
                        EXECUTE 'REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM authenticated';
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE {table} DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS redact_patient_audit_values(uuid);");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_audit_logs_immutable ON audit_logs;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS audit_logs_immutable();");
        }
    }
}
