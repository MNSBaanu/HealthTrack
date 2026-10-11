"""Admin assignment integration checks against a disposable SQL Server database."""
import argparse
import html
import json
import re
import subprocess
from pathlib import Path
import appointments_functional as http

http.BASE = 'http://localhost:57625'

def main(args):
    assert re.fullmatch(r'HealthTrack_Assignments_Test_[a-f0-9]{32}', args.database)
    def sql(query):
        result = subprocess.run(['sqlcmd', '-S', args.server, '-d', args.database, '-E', '-I',
            '-b', '-W', '-h', '-1', '-Q', 'SET NOCOUNT ON; ' + query], capture_output=True, text=True)
        if result.returncode: raise RuntimeError(result.stdout + result.stderr)
        return result.stdout.strip()

    def id_for(email):
        return sql(f"SELECT Id FROM Users WHERE Email='{email}'")

    def version(client_id):
        page = admin.request('/Assignments/Edit/' + client_id)[2]
        match = re.search(r'name="RowVersion"[^>]*value="([^"]*)"', page)
        return html.unescape(match[1]) if match else ''

    def save(client_id, trainer_id, row_version=None, session=None):
        session = session or admin
        fields = dict(ClientId=client_id, TrainerId=trainer_id,
                      RowVersion=version(client_id) if row_version is None else row_version)
        fields['__RequestVerificationToken'] = session.token('/Assignments/Edit/' + client_id)
        return session.request('/Assignments/Edit', fields)

    def remove(client_id, row_version):
        return admin.request('/Assignments/Remove', dict(ClientId=client_id, RowVersion=row_version,
            __RequestVerificationToken=admin.token('/Assignments/Edit/' + client_id)))

    def dashboard_count(key, count):
        return f'data-stat="{key}">{count}</div>' in admin.request('/Admin')[2]

    def feed(session):
        code, _, page = session.request('/TrainerAssignments')
        assert code == 200, code
        return json.loads(page)

    check = http.check
    admin, client, client2, trainer1, trainer2, anon = [http.Session() for _ in range(6)]
    admin.post('/Account/Login', dict(Email='admin.assignments.test@healthtrack.example', Password=http.PASSWORD))
    http.register(client, 'client.assignments@healthtrack.example')
    http.register(client2, 'client2.assignments@healthtrack.example')
    early_client_id = id_for('client.assignments@healthtrack.example')
    check('No-Trainer assignment empty state', 'No unlocked Trainer accounts are available' in admin.request('/Assignments/Edit/' + early_client_id)[2])
    trainer_sessions = [http.Session() for _ in range(3)]
    for index, session in enumerate(trainer_sessions, 1):
        http.register(session, f'trainer{index}.assignments@healthtrack.example')
    # Fixture-only promotion. Production Trainer registration belongs to the Trainer module.
    sql("""
        DELETE HealthMetrics WHERE ClientId IN (SELECT Id FROM Users WHERE Email LIKE 'trainer%.assignments@healthtrack.example');
        UPDATE Users SET Role='Trainer', HeightCm=NULL, Name=CASE
            WHEN Email LIKE 'trainer1.%' THEN 'QA Trainer One'
            WHEN Email LIKE 'trainer2.%' THEN 'QA Trainer Two' ELSE 'QA NoRole Trainer' END
            WHERE Email LIKE 'trainer%.assignments@healthtrack.example';
        DELETE UserRoles WHERE UserId IN (SELECT Id FROM Users WHERE Role='Trainer');
        INSERT UserRoles (UserId,RoleId) SELECT u.Id,r.Id FROM Users u CROSS JOIN Roles r
            WHERE u.Role='Trainer' AND u.Email NOT LIKE 'trainer3.%' AND r.Name='Trainer';
    """)
    trainer1.post('/Account/Login', dict(Email='trainer1.assignments@healthtrack.example', Password=http.PASSWORD))
    trainer2.post('/Account/Login', dict(Email='trainer2.assignments@healthtrack.example', Password=http.PASSWORD))
    cid, cid2 = id_for('client.assignments@healthtrack.example'), id_for('client2.assignments@healthtrack.example')
    tid1, tid2, tid3 = [id_for(f'trainer{i}.assignments@healthtrack.example') for i in range(1,4)]
    aid = id_for('admin.assignments.test@healthtrack.example')
    check('Anonymous denied assignment list', anon.request('/Assignments')[0] == 302)
    check('Client denied assignment list and edit', client.request('/Assignments')[0] == 302 and client.request('/Assignments/Edit/' + cid)[0] == 302)
    check('Client denied assignment mutation', client.request('/Assignments/Edit', dict(ClientId=cid,TrainerId=tid1))[0] == 302)
    check('Client denied assignment removal', client.request('/Assignments/Remove', dict(ClientId=cid,RowVersion='anything'))[0] == 302)
    check('Trainer denied Admin assignment management', trainer1.request('/Assignments')[0] == 302)
    check('Client denied Trainer feed', client.request('/TrainerAssignments')[0] == 302)
    check('Trainer login avoids redirect loop', trainer1.request('/Home')[0] == 200 and trainer1.request('/Home/Dashboard')[0] == 200)
    check('Initially unassigned Clients counted', dashboard_count('assigned',0) and dashboard_count('unassigned',2))
    check('Trainer count reflects shared identity', dashboard_count('trainers',3))
    check('Admin Trainer role filter includes Trainer accounts', 'QA Trainer One' in admin.request('/Admin/Users?role=Trainer')[2])
    edit = admin.request('/Assignments/Edit/' + cid)[2]
    check('Eligible Trainer options exclude missing role', 'QA Trainer One' in edit and 'QA NoRole Trainer' not in edit)
    check('Wrong Client type rejected', admin.request('/Assignments/Edit/' + aid)[0] == 404)
    check('Client selected as Trainer rejected', http.invalid(save(cid,cid2)))
    check('Admin selected as Trainer rejected', http.invalid(save(cid,aid)))
    check('Trainer without Identity role rejected', http.invalid(save(cid,tid3)))
    check('Missing Trainer rejected', http.invalid(save(cid,'missing')))
    check('Assignment form requires antiforgery token', admin.request('/Assignments/Edit',dict(ClientId=cid,TrainerId=tid1))[0] == 400)
    response = save(cid,tid1)
    check('Valid assignment saved', response[0] == 302 and response[1].get('Location') == '/Assignments')
    check('One assignment persisted for Client', sql(f"SELECT COUNT(*) FROM ClientTrainerAssignments WHERE ClientId='{cid}' AND TrainerId='{tid1}'") == '1')
    check('Client sees assigned Trainer', 'QA Trainer One' in client.request('/Client')[2])
    check('Only assigned Trainer sees Client in feed', len(feed(trainer1)) == 1 and len(feed(trainer2)) == 0)
    check('Assigned and unassigned counts match', dashboard_count('assigned',1) and dashboard_count('unassigned',1))
    check('Unassigned filter excludes assigned Client', cid not in admin.request('/Assignments?unassignedOnly=true')[2] and cid2 in admin.request('/Assignments?unassignedOnly=true')[2])
    check('Admin Client detail displays assignment', 'QA Trainer One' in admin.request('/Admin/ClientDetails/' + cid)[2])
    check('Duplicate Trainer assignment rejected', http.invalid(save(cid,tid1)))
    sql(f"UPDATE Users SET LockoutEnd='9999-12-31T23:59:59+00:00' WHERE Id='{tid2}'")
    check('Locked Trainer rejected and excluded from choices', http.invalid(save(cid,tid2)) and 'QA Trainer Two' not in admin.request('/Assignments/Edit/' + cid)[2])
    sql(f"UPDATE Users SET LockoutEnd=NULL WHERE Id='{tid2}'")
    # Existing consultation data must stay with its original provider on reassignment.
    sql(f"""
        INSERT ConsultationProviders(Name,Kind,IsActive,PersonId) VALUES('Original Trainer',0,1,'{tid1}');
        INSERT AvailabilitySlots(ProviderId,StartUtc,EndUtc,IsAvailable) VALUES
            (1,DATEADD(day,3,SYSUTCDATETIME()),DATEADD(minute,30,DATEADD(day,3,SYSUTCDATETIME())),1);
        INSERT Appointments(ClientId,SlotId,Status,RequestedAtUtc) VALUES('{cid}',1,1,SYSUTCDATETIME());
    """)
    before = sql(f"SELECT CONCAT(a.Id,':',a.SlotId,':',a.Status,':',s.ProviderId) FROM Appointments a JOIN AvailabilitySlots s ON s.Id=a.SlotId WHERE ClientId='{cid}'")
    old_version = version(cid)
    check('Reassignment succeeds', save(cid,tid2,old_version)[1].get('Location') == '/Assignments')
    check('Reassignment immediately updates Client and Trainer reads', 'QA Trainer Two' in client.request('/Client')[2] and len(feed(trainer1)) == 0 and len(feed(trainer2)) == 1)
    after = sql(f"SELECT CONCAT(a.Id,':',a.SlotId,':',a.Status,':',s.ProviderId) FROM Appointments a JOIN AvailabilitySlots s ON s.Id=a.SlotId WHERE ClientId='{cid}'")
    check('Existing appointment provider/status unchanged by reassignment', before == after)
    check('Stale reassignment rejected without overwriting', save(cid,tid1,old_version)[1].get('Location','').startswith('/Assignments/Edit/') and sql(f"SELECT TrainerId FROM ClientTrainerAssignments WHERE ClientId='{cid}'") == tid2)
    check('Stale removal rejected', remove(cid,old_version)[1].get('Location','').startswith('/Assignments/Edit/') and sql(f"SELECT COUNT(*) FROM ClientTrainerAssignments WHERE ClientId='{cid}'") == '1')
    check('Malformed rowversion rejected', save(cid,tid1,'not-base64')[1].get('Location','').startswith('/Assignments/Edit/'))
    check('Assigned Trainer deletion blocked', admin.request('/Admin/DeleteUser/' + tid2,
        dict(__RequestVerificationToken=admin.token('/Admin/Users')))[0] == 302
        and sql(f"SELECT COUNT(*) FROM Users WHERE Id='{tid2}'") == '1')
    current = version(cid)
    check('Removal succeeds', remove(cid,current)[1].get('Location') == '/Assignments')
    check('Removal reflected in Client, Trainer, and dashboard', 'No Trainer is assigned' in client.request('/Client')[2] and len(feed(trainer2)) == 0 and dashboard_count('assigned',0) and dashboard_count('unassigned',2))
    check('Existing appointment preserved after removal', sql(f"SELECT CONCAT(a.Id,':',a.SlotId,':',a.Status,':',s.ProviderId) FROM Appointments a JOIN AvailabilitySlots s ON s.Id=a.SlotId WHERE ClientId='{cid}'") == before)
    check('Client appointments and logging still work', client.request('/Appointments')[0] == 200 and client.request('/WorkoutLogs')[0] == 200 and client.request('/MealLogs')[0] == 200)
    check('All assignments still satisfy one Trainer per Client', sql('SELECT COUNT(*) FROM (SELECT ClientId FROM ClientTrainerAssignments GROUP BY ClientId HAVING COUNT(*)>1) d') == '0')
    output = Path(__file__).resolve().parents[1] / 'artifacts' / 'assignments' / 'results.json'
    output.write_text(json.dumps(http.RESULTS,indent=2))
    return 0 if all(r['passed'] for r in http.RESULTS) else 1

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--database', required=True)
    parser.add_argument('--server', required=True)
    raise SystemExit(main(parser.parse_args()))
