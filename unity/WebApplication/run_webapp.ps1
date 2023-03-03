# read config file
$config = Get-Content -Raw -Path "../Config/AppConfig.json" | ConvertFrom-Json

# run backend 
invoke-expression "cmd /c start run_backend.bat $($config.CamerafyBackendUrl):$($config.CamerafyBackendPort) '""http://localhost:$($config.CamerafyPort)/api""' $(If($config.UserLogin) { 0 } Else { 1 })"

# run frontend
invoke-expression "cmd /c start run_frontend.bat $($config.CamerafyFrontendUrl) $($config.CamerafyFrontendPort)"