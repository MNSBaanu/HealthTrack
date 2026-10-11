"""Real HTTP/SQL booking checks. Run through Run-AppointmentTests.ps1 only."""
import argparse
import concurrent.futures
import datetime as dt
import html
import http.cookiejar
import json
import re
import subprocess
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

BASE = 'http://localhost:57623'
PASSWORD = 'TestOnly!Pass2026'
RESULTS = []

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None

class Session:
    def __init__(self):
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect())

    def request(self, path, data=None):
        body = urllib.parse.urlencode(data).encode() if data is not None else None
        try:
            response = self.opener.open(urllib.request.Request(BASE + path, data=body), timeout=45)
        except urllib.error.HTTPError as error:
            response = error
        return response.code, response.headers, response.read().decode('utf-8-sig')

    def token(self, path):
        code, _, page = self.request(path)
        match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
        assert code == 200 and match, (code, path)
        return html.unescape(match[1])

    def post(self, path, data):
        return self.request(path, dict(data, __RequestVerificationToken=self.token(path)))

def check(name, condition):
    RESULTS.append({'test': name, 'passed': bool(condition)})
    print(('PASS ' if condition else 'FAIL ') + name, flush=True)

def saved(response):
    return response[0] == 302 and response[1].get('Location', '').startswith('/Appointments/Details/')

def invalid(response):
    return response[0] == 200 and 'validation-summary-errors' in response[2]

def register(session, email):
    response = session.post('/Account/Register', dict(Name='Appointment QA', Email=email,
        DateOfBirth='1995-01-15', HeightCm=175, WeightKg=80, Password=PASSWORD, ConfirmPassword=PASSWORD))
    assert response[0] == 302, response[2][:300]

def main(args):
    assert re.fullmatch(r'HealthTrack_Appointments_Test_[a-f0-9]{32}', args.database)
    def sql(query):
        result = subprocess.run(['sqlcmd', '-S', args.server, '-d', args.database,
            '-E', '-I', '-b', '-W', '-h', '-1', '-Q', 'SET NOCOUNT ON; ' + query],
            capture_output=True, text=True)
        if result.returncode:
            raise RuntimeError(result.stdout + result.stderr)
        return result.stdout.strip()

    anonymous, alpha, beta, admin = Session(), Session(), Session(), Session()
    check('Anonymous cannot access appointments', anonymous.request('/Appointments')[0] == 302)
    register(alpha, 'alpha.appointments@healthtrack.example')
    register(beta, 'beta.appointments@healthtrack.example')
    admin.post('/Account/Login', dict(Email='admin.appointments.test@healthtrack.example', Password=PASSWORD))
    check('Admin cannot access Client booking', admin.request('/Appointments/Book')[0] == 302)
    check('No-provider booking empty state', 'No consultation providers' in alpha.request('/Appointments/Book')[2])
    check('Appointment list empty state', 'You have no appointments yet' in alpha.request('/Appointments')[2])
    check('Dashboard works without appointments', 'No upcoming appointments' in alpha.request('/Client')[2])

    sql("""
        INSERT ConsultationProviders (Name, Specialty, Kind, IsActive) VALUES
        ('QA Trainer', 'Fitness', 0, 1), ('QA Clinician', 'Nutrition', 1, 1),
        ('QA Disabled', NULL, 0, 0), ('QA Concurrent', NULL, 0, 1);
        DECLARE @base datetime2 = DATEADD(day, 2, SYSUTCDATETIME());
        INSERT AvailabilitySlots (ProviderId, StartUtc, EndUtc, IsAvailable) VALUES
        (1, @base, DATEADD(minute, 60, @base), 1),
        (1, DATEADD(minute, 30, @base), DATEADD(minute, 90, @base), 1),
        (2, DATEADD(minute, 30, @base), DATEADD(minute, 90, @base), 1),
        (2, DATEADD(minute, 60, @base), DATEADD(minute, 120, @base), 1),
        (1, DATEADD(day, -3, @base), DATEADD(day, -3, DATEADD(minute, 60, @base)), 1),
        (1, DATEADD(day, 1, @base), DATEADD(day, 1, DATEADD(minute, 60, @base)), 0),
        (3, @base, DATEADD(minute, 60, @base), 1),
        (1, DATEADD(day, 2, @base), DATEADD(day, 2, DATEADD(minute, 60, @base)), 1),
        (4, @base, DATEADD(minute, 60, @base), 1),
        (4, DATEADD(minute, 30, @base), DATEADD(minute, 90, @base), 1);
    """)
    page = alpha.request('/Appointments/Book')[2]
    check('Provider types and timezone displayed', all(x in page for x in ['QA Trainer', 'QA Clinician', 'Healthcare professional', 'Asia/Colombo']))
    check('Inactive providers hidden', 'QA Disabled' not in page)
    filtered = alpha.request('/Appointments/Book?providerId=2')[2]
    check('Provider filter limits selectable slots', 'value="1"' not in re.search(r'<select[^>]*id="SlotId".*?</select>', filtered, re.S).group())
    check('Missing slot rejected', invalid(alpha.post('/Appointments/Book', {})))
    check('Missing antiforgery token rejected', alpha.request('/Appointments/Book', dict(SlotId=1))[0] == 400)
    check('Invalid slot rejected', invalid(alpha.post('/Appointments/Book', dict(SlotId=99999))))
    check('Past slot rejected', invalid(alpha.post('/Appointments/Book', dict(SlotId=5))))
    check('Unavailable slot rejected', invalid(alpha.post('/Appointments/Book', dict(SlotId=6))))
    check('Inactive provider booking rejected', invalid(alpha.post('/Appointments/Book', dict(SlotId=7))))
    check('Oversized notes rejected', invalid(alpha.post('/Appointments/Book', dict(SlotId=1, Notes='x'*2001))))
    result = alpha.post('/Appointments/Book', dict(SlotId=1, Notes='<script>unsafe</script>', ClientId='other', Status=1))
    check('Valid consultation persisted', saved(result))
    detail_path = result[1].get('Location', '')
    appointment_id = int(detail_path.rsplit('/', 1)[1])
    check('Server derives owner and Pending status', sql(f"SELECT COUNT(*) FROM Appointments a JOIN Users u ON u.Id=a.ClientId WHERE a.Id={appointment_id} AND u.Email='alpha.appointments@healthtrack.example' AND a.Status=0") == '1')
    details = alpha.request(detail_path)[2]
    check('Details show provider and escaped notes', 'QA Trainer' in details and '&lt;script&gt;' in details and '<script>unsafe' not in details)
    check('Other Client details rejected', beta.request(detail_path)[0] == 404)
    check('Other Client list excludes appointment', 'QA Trainer' not in beta.request('/Appointments')[2])
    check('Upcoming dashboard includes booking', detail_path in alpha.request('/Client')[2])
    check('Duplicate slot rejected', invalid(beta.post('/Appointments/Book', dict(SlotId=1))))
    check('Provider overlap rejected across slots', invalid(beta.post('/Appointments/Book', dict(SlotId=2))))
    check('Client overlap rejected across providers', invalid(alpha.post('/Appointments/Book', dict(SlotId=3))))
    check('Adjacent times allowed', saved(alpha.post('/Appointments/Book', dict(SlotId=4))))

    sql(f'UPDATE Appointments SET Status=1 WHERE Id={appointment_id}')
    check('Trainer approval reflected in Client details', 'Approved' in alpha.request(detail_path)[2])
    sql(f'UPDATE Appointments SET SlotId=8 WHERE Id={appointment_id}')
    changed = dt.datetime.fromisoformat(sql('SELECT CONVERT(varchar(30), StartUtc, 126) FROM AvailabilitySlots WHERE Id=8')) + dt.timedelta(hours=5, minutes=30)
    detail_time = f'{changed.day} {changed:%b %Y %H:%M}'
    dashboard_time = f'{changed.day} {changed:%b, %H:%M}'
    check('Trainer reschedule reflected in dashboard and details',
          detail_time in alpha.request(detail_path)[2] and dashboard_time in alpha.request('/Client')[2])
    sql(f'UPDATE Appointments SET Status=2 WHERE Id={appointment_id}')
    check('Cancellation retained in Client list', 'Cancelled' in alpha.request('/Appointments')[2])
    check('Cancellation removed from upcoming dashboard', detail_path not in alpha.request('/Client')[2])
    check('Cancelled slot can be booked again', saved(beta.post('/Appointments/Book', dict(SlotId=8))))

    token_a, token_b = alpha.token('/Appointments/Book'), beta.token('/Appointments/Book')
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        futures = [pool.submit(alpha.request, '/Appointments/Book', dict(SlotId=9, __RequestVerificationToken=token_a)),
                   pool.submit(beta.request, '/Appointments/Book', dict(SlotId=10, __RequestVerificationToken=token_b))]
        results = [f.result() for f in futures]
    check('Concurrent overlapping requests yield one booking and useful rejection', sum(saved(r) for r in results) == 1 and sum(invalid(r) for r in results) == 1)
    check('Database contains only one concurrent reservation', sql('SELECT COUNT(*) FROM Appointments WHERE SlotId IN (9,10) AND Status<>2') == '1')
    for path in ['/WorkoutLogs', '/MealLogs', '/HealthMetrics', '/Client/Progress']:
        check('Existing Client page works: ' + path, alpha.request(path)[0] == 200)
    output = Path(__file__).resolve().parents[1] / 'artifacts' / 'appointments' / 'results.json'
    output.write_text(json.dumps(RESULTS, indent=2))
    return 0 if all(r['passed'] for r in RESULTS) else 1

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--database', required=True)
    parser.add_argument('--server', required=True)
    raise SystemExit(main(parser.parse_args()))
