DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*)
    INTO v_count
    FROM WORKFLOWGLOBALPARAMETER
    WHERE LENGTH(TYPE) > 128;

    IF v_count > 0 THEN
        raise_application_error(-20000,
            'BREAKING CHANGES DETECTED: Some rows in the TYPE column in WORKFLOWGLOBALPARAMETER table are too long. Please contact support support@optimajet.com.');
    END IF;

    SELECT COUNT(*)
    INTO v_count
    FROM WORKFLOWPROCESSINSTANCE
    WHERE LENGTH(TENANTID) > 128;

    IF v_count > 0 THEN
        raise_application_error(-20001,
            'BREAKING CHANGES DETECTED: Some rows in the TENANTID column in WORKFLOWPROCESSINSTANCE table are too long. Please contact support support@optimajet.com.');
    END IF;
END;
