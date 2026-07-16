UPDATE `workflowscheme`
SET `Id` = UUID_TO_BIN(UUID())
WHERE `Id` IS NULL;
