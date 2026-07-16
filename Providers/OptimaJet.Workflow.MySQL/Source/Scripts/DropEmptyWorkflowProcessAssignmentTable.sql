DROP PROCEDURE IF EXISTS `workflow_drop_empty_assignment_table`;

CREATE PROCEDURE `workflow_drop_empty_assignment_table`()
BEGIN
    DECLARE assignment_table_exists BOOLEAN DEFAULT FALSE;
    DECLARE assignment_table_has_rows BOOLEAN DEFAULT FALSE;

    SELECT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = DATABASE()
          AND table_name = 'workflowprocessassignment'
    ) INTO assignment_table_exists;

    IF assignment_table_exists THEN
        SELECT EXISTS (SELECT 1 FROM `workflowprocessassignment`)
        INTO assignment_table_has_rows;

        IF NOT assignment_table_has_rows THEN
            DROP TABLE `workflowprocessassignment`;
        END IF;
    END IF;
END;

CALL `workflow_drop_empty_assignment_table`();

DROP PROCEDURE `workflow_drop_empty_assignment_table`;
