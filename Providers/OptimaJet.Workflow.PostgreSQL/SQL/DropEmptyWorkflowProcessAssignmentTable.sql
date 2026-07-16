DO $$
DECLARE
    assignment_table_has_rows boolean := false;
BEGIN
    IF to_regclass('"WorkflowProcessAssignment"') IS NOT NULL THEN
        EXECUTE 'SELECT EXISTS (SELECT 1 FROM "WorkflowProcessAssignment")'
            INTO assignment_table_has_rows;

        IF NOT assignment_table_has_rows THEN
            EXECUTE 'DROP TABLE "WorkflowProcessAssignment"';
        END IF;
    END IF;
END
$$;
