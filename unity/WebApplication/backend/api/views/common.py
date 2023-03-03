
def verify_request_data(request_data, required_data = []):
    """
    Returns a list of all missing data field in request_data.
    """
    data_fields = list(request_data.keys())
    missing = []
    for r in required_data:
        if r not in data_fields:
            missing.append(r)

    return missing