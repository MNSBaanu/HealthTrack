"""Admin security regressions; use Run-AdminSecurityTests.ps1 on a disposable DB."""
import argparse
import json
import re
import subprocess
from pathlib import Path
import appointments_functional as http

http.BASE = 'http://localhost:57631'

def fields(email):
    return dict(Name='Security QA Admin', Email=email, DateOfBirth='1990-01-01',
                Password=http.PASSWORD, ConfirmPassword=http.PASSWORD)

def main(args):
    assert re.fullmatch(r'HealthTrack_AdminSecurity_Test_[a-f0-9]{32}', args.database)
    def sql(query):
        result = subprocess.run(['sqlcmd', '-S', args.server, '-d', args.database, '-E', '-I', '-b', '-W', '-h', '-1', '-Q', 'SET NOCOUNT ON; '+query], capture_output=True, text=True)
        if result.returncode: raise RuntimeError(result.stdout+result.stderr)
        return result.stdout.strip()
    def uid(email):
        return sql(f"SELECT Id FROM Users WHERE Email='{email}'")
    def login(session, email):
        return session.post('/Account/Login', dict(Email=email, Password=http.PASSWORD))
    def mutate(path, data):
        return admin.request(path, dict(data, __RequestVerificationToken=admin.token('/Admin/Users')))
    def revoked(session, path, body=None):
        response = session.request(path, body)
        return response[0] == 302 and '/Account/Login' in response[1].get('Location', '')
    admin, victim, client, observer = [http.Session() for _ in range(4)]
    check = http.check
    login(admin, 'admin.adminsecurity.test@healthtrack.example')
    email = 'victim.adminsecurity@healthtrack.example'
    check('Create Admin has one role and no access-level selector', 'AccessLevel' not in admin.request('/Admin/CreateAdmin')[2])
    check('Create Admin ignores obsolete access-level input', admin.post('/Admin/CreateAdmin', dict(fields(email), AccessLevel=3))[0] == 302 and sql(f"SELECT AccessLevel FROM Users WHERE Email='{email}'") == '1')
    login(victim, email)
    login(observer, email)
    vid = uid(email)
    http.register(client, 'client.adminsecurity@healthtrack.example')
    cid = uid('client.adminsecurity@healthtrack.example')
    token = victim.token('/Admin/CreateAdmin')
    mutate('/Admin/ToggleLock', dict(id=vid))
    check('Locked Admin read session immediately revoked', revoked(victim, '/Admin/Reports'))
    check('Locked Admin second session cannot mutate with old CSRF token', revoked(observer, '/Admin/CreateAdmin', dict(fields('lockedwrite.adminsecurity@healthtrack.example'), __RequestVerificationToken=token)))
    check('Blocked mutation creates no account', uid('lockedwrite.adminsecurity@healthtrack.example') == '')
    check('Locked Admin new login rejected', 'This account is locked' in login(http.Session(), email)[2])
    mutate('/Admin/ToggleLock', dict(id=vid))
    check('Unlocked Admin can sign in again', login(victim, email)[0] == 302 and victim.request('/Admin')[0] == 200)
    login(observer, email)
    token = victim.token('/Admin/CreateAdmin')
    mutate('/Admin/DeleteUser', dict(id=vid))
    check('Deleted Admin cannot export using old cookie', revoked(victim, '/Admin/ExportReport?format=csv'))
    check('Deleted Admin cannot create accounts using old cookie', revoked(observer, '/Admin/CreateAdmin', dict(fields('deletedwrite.adminsecurity@healthtrack.example'), __RequestVerificationToken=token)))
    check('Deleted Admin mutation creates no account', uid('deletedwrite.adminsecurity@healthtrack.example') == '')
    # Check lockout itself, even if no security-stamp update accompanies it.
    http.register(observer, 'locked.client.adminsecurity@healthtrack.example')
    locked_cid = uid('locked.client.adminsecurity@healthtrack.example')
    sql(f"UPDATE Users SET LockoutEnd='9999-12-31T23:59:59+00:00' WHERE Id='{locked_cid}'")
    check('Lock state checked independently of security-stamp changes', revoked(observer, '/Client'))
    mutate('/Admin/DeleteUser', dict(id=cid))
    check('Deleted Client dashboard redirects to login rather than HTTP 500', revoked(client, '/Client'))
    http.register(client, 'recipient.adminsecurity@healthtrack.example')
    recipient_id = uid('recipient.adminsecurity@healthtrack.example')
    notification = dict(Message='Security QA message', Type=0)
    result = admin.post('/Admin/SendNotification', dict(notification, RecipientId=uid('admin.adminsecurity.test@healthtrack.example')))
    check('Admin recipient ID rejected with visible error', result[0] == 200 and 'Choose a valid Client recipient' in result[2])
    # Promote a fixture Client to Trainer only within the test database.
    http.register(observer, 'trainer.adminsecurity@healthtrack.example')
    trainer_id = uid('trainer.adminsecurity@healthtrack.example')
    sql(f"DELETE HealthMetrics WHERE ClientId='{trainer_id}'; UPDATE Users SET Role='Trainer', HeightCm=NULL WHERE Id='{trainer_id}';")
    result = admin.post('/Admin/SendNotification', dict(notification, RecipientId=trainer_id))
    check('Trainer recipient ID rejected', result[0] == 200 and 'Choose a valid Client recipient' in result[2])
    check('Rejected recipient submissions persist no notifications', sql("SELECT COUNT(*) FROM Notifications WHERE Message='Security QA message'") == '0')
    result = admin.post('/Admin/SendNotification', dict(notification, Type=999, RecipientId=recipient_id))
    check('Invalid notification type visibly identifies Type field', result[0] == 200 and 'data-valmsg-for="Type"' in result[2] and 'field-validation-error' in result[2])
    check('Invalid notification type persists no notification', sql("SELECT COUNT(*) FROM Notifications WHERE Message='Security QA message'") == '0')
    check('Valid Client notification still succeeds', admin.post('/Admin/SendNotification', dict(notification, RecipientId=recipient_id))[0] == 302 and 'Security QA message' in client.request('/Notifications')[2])
    check('Client broadcast still succeeds', admin.post('/Admin/SendNotification', dict(Message='Security QA broadcast', Type=0))[0] == 302 and 'Security QA broadcast' in client.request('/Notifications')[2])
    out = Path(__file__).resolve().parents[1] / 'artifacts/adminsecurity/results.json'
    out.write_text(json.dumps(http.RESULTS, indent=2), encoding='utf-8')
    return 0 if all(r['passed'] for r in http.RESULTS) else 1

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--database', required=True)
    parser.add_argument('--server', required=True)
    raise SystemExit(main(parser.parse_args()))
