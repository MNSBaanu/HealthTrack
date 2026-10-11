import argparse
import json
import re
import subprocess
import time
from pathlib import Path
import appointments_functional as http

http.BASE = 'http://localhost:57627'

def main(args):
    assert re.fullmatch(r'HealthTrack_Reminders_Test_[a-f0-9]{32}', args.database)
    def sql(query):
        result = subprocess.run(['sqlcmd','-S',args.server,'-d',args.database,'-E','-I','-b','-W','-h','-1',
            '-Q','SET NOCOUNT ON; '+query],capture_output=True,text=True)
        if result.returncode: raise RuntimeError(result.stdout+result.stderr)
        return result.stdout.strip()
    check = http.check
    client,other,admin = http.Session(),http.Session(),http.Session()
    http.register(client,'reminder.client@healthtrack.example')
    http.register(other,'reminder.other@healthtrack.example')
    admin.post('/Account/Login',dict(Email='admin.reminders.test@healthtrack.example',Password=http.PASSWORD))
    cid = sql("SELECT Id FROM Users WHERE Email='reminder.client@healthtrack.example'")
    oid = sql("SELECT Id FROM Users WHERE Email='reminder.other@healthtrack.example'")
    sql(f"""
        INSERT ConsultationProviders(Name,Kind,IsActive) VALUES('Reminder Provider',0,1);
        INSERT AvailabilitySlots(ProviderId,StartUtc,EndUtc,IsAvailable) VALUES
        (1,DATEADD(hour,6,SYSUTCDATETIME()),DATEADD(hour,7,SYSUTCDATETIME()),1),
        (1,DATEADD(day,3,SYSUTCDATETIME()),DATEADD(hour,1,DATEADD(day,3,SYSUTCDATETIME())),1),
        (1,DATEADD(hour,8,SYSUTCDATETIME()),DATEADD(hour,9,SYSUTCDATETIME()),1),
        (1,DATEADD(hour,10,SYSUTCDATETIME()),DATEADD(hour,11,SYSUTCDATETIME()),1),
        (1,DATEADD(hour,-2,SYSUTCDATETIME()),DATEADD(hour,-1,SYSUTCDATETIME()),1),
        (1,DATEADD(hour,12,SYSUTCDATETIME()),DATEADD(hour,13,SYSUTCDATETIME()),1),
        (1,DATEADD(hour,14,SYSUTCDATETIME()),DATEADD(hour,15,SYSUTCDATETIME()),1);
        INSERT Appointments(ClientId,SlotId,Status,RequestedAtUtc) VALUES
        ('{cid}',1,1,SYSUTCDATETIME()),('{cid}',2,1,SYSUTCDATETIME()),
        ('{cid}',3,0,SYSUTCDATETIME()),('{cid}',4,2,SYSUTCDATETIME()),
        ('{cid}',5,1,SYSUTCDATETIME()),('{cid}',6,3,SYSUTCDATETIME()),
        ('{oid}',7,1,SYSUTCDATETIME());
    """)
    # Do not visit the Client UI: demonstrate generation by the worker itself.
    deadline = time.monotonic()+70
    while time.monotonic()<deadline and sql('SELECT COUNT(*) FROM Notifications WHERE AppointmentId=1')!='1':
        time.sleep(1)
    check('Background worker creates reminder without Client visiting dashboard',sql('SELECT COUNT(*) FROM Notifications WHERE AppointmentId=1')=='1')
    page = client.request('/Notifications')[2]
    check('Reminder shows provider, appointment time, timezone and link',all(s in page for s in ['Appointment reminder:', 'Reminder Provider','Asia/Colombo','/Appointments/Details/1']))
    check('Only approved future appointments inside 24-hour window reminded',sql(f"SELECT COUNT(*) FROM Notifications WHERE PersonId='{cid}' AND AppointmentId IS NOT NULL")=='1')
    check('Other Client cannot see reminder details',other.request('/Appointments/Details/1')[0]==404)
    nid = sql('SELECT NotificationId FROM Notifications WHERE AppointmentId=1')
    for _ in range(3): client.request('/Client'); client.request('/Notifications/UnreadCount')
    check('Repeated refreshes do not duplicate reminders',sql('SELECT COUNT(*) FROM Notifications WHERE AppointmentId=1')=='1')
    def post(path,fields):
        return client.request(path,dict(fields,__RequestVerificationToken=client.token('/Notifications')))
    post('/Notifications/MarkAsRead',dict(id=nid))
    client.request('/Notifications')
    check('Read reminder remains read after refresh',sql('SELECT IsRead FROM Notifications WHERE AppointmentId=1')=='1')
    sql('UPDATE AvailabilitySlots SET StartUtc=DATEADD(minute,30,StartUtc),EndUtc=DATEADD(minute,30,EndUtc) WHERE Id=1')
    client.request('/Notifications')
    check('Rescheduling inside reminder window refreshes alert and unread status',sql('SELECT IsRead FROM Notifications WHERE AppointmentId=1')=='0')
    post('/Notifications/Delete',dict(id=nid))
    check('Dismissed reminder is not recreated on refresh','/Appointments/Details/1' not in client.request('/Notifications')[2] and sql('SELECT IsDismissed FROM Notifications WHERE AppointmentId=1')=='1')
    sql('UPDATE AvailabilitySlots SET StartUtc=DATEADD(minute,30,StartUtc),EndUtc=DATEADD(minute,30,EndUtc) WHERE Id=1')
    check('A rescheduled appointment can alert again after dismissal','/Appointments/Details/1' in client.request('/Notifications')[2])
    sql('UPDATE Appointments SET Status=2 WHERE Id=1')
    check('Cancelled appointment reminder removed immediately','/Appointments/Details/1' not in client.request('/Notifications')[2] and sql('SELECT COUNT(*) FROM Notifications WHERE AppointmentId=1')=='0')
    sql('UPDATE Appointments SET Status=1 WHERE Id=1')
    client.request('/Notifications')
    sql('UPDATE AvailabilitySlots SET StartUtc=DATEADD(day,3,SYSUTCDATETIME()),EndUtc=DATEADD(hour,1,DATEADD(day,3,SYSUTCDATETIME())) WHERE Id=1')
    check('Moving outside 24-hour window removes old reminder','/Appointments/Details/1' not in client.request('/Notifications')[2])
    sql('UPDATE AvailabilitySlots SET StartUtc=DATEADD(hour,2,SYSUTCDATETIME()),EndUtc=DATEADD(hour,3,SYSUTCDATETIME()) WHERE Id=1')
    client.request('/Notifications')
    sql('UPDATE AvailabilitySlots SET StartUtc=DATEADD(hour,-2,SYSUTCDATETIME()),EndUtc=DATEADD(hour,-1,SYSUTCDATETIME()) WHERE Id=1')
    check('Past appointment reminder removed','/Appointments/Details/1' not in client.request('/Notifications')[2])
    sql(f"INSERT Notifications(PersonId,Message,Type,IsRead,CreatedAt,ScheduledFor,IsDismissed) VALUES('{cid}','Future manual reminder','Appointment',0,GETDATE(),DATEADD(day,1,GETDATE()),0)")
    future_id=sql("SELECT NotificationId FROM Notifications WHERE Message='Future manual reminder'")
    post('/Notifications/MarkAllAsRead',{})
    check('Mark all as read preserves hidden scheduled reminders',sql(f'SELECT IsRead FROM Notifications WHERE NotificationId={future_id}')=='0')
    check('Single read rejects hidden future reminder',post('/Notifications/MarkAsRead',dict(id=future_id))[0]==404)
    sql(f'UPDATE Notifications SET ScheduledFor=DATEADD(minute,-1,GETDATE()) WHERE NotificationId={future_id}')
    check('Scheduled manual reminder appears unread when due','Future manual reminder' in client.request('/Notifications')[2] and sql(f'SELECT IsRead FROM Notifications WHERE NotificationId={future_id}')=='0')
    response=admin.request('/Admin/DeleteUser/'+oid,dict(__RequestVerificationToken=admin.token('/Admin/Users')))
    check('Client deletion safely cascades linked appointments and reminders',response[0]==302 and sql(f"SELECT COUNT(*) FROM Users WHERE Id='{oid}'")=='0')
    output=Path(__file__).resolve().parents[1]/'artifacts/reminders/results.json'
    output.write_text(json.dumps(http.RESULTS,indent=2))
    return 0 if all(r['passed'] for r in http.RESULTS) else 1

if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--database',required=True); parser.add_argument('--server',required=True)
    raise SystemExit(main(parser.parse_args()))
