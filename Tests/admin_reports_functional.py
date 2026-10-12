"""B05 acceptance checks; use Run-AdminReportTests.ps1 with a disposable database."""
import argparse
import csv
import io
import json
import re
import subprocess
from pathlib import Path
from pypdf import PdfReader
import appointments_functional as h
h.BASE='http://localhost:57635'

def main(args):
    assert re.fullmatch(r'HealthTrack_AdminReports_Test_[a-f0-9]{32}',args.database)
    def sql(query):
        r=subprocess.run(['sqlcmd','-S',args.server,'-d',args.database,'-E','-I','-b','-W','-h','-1','-Q','SET NOCOUNT ON; '+query],capture_output=True,text=True)
        if r.returncode:raise RuntimeError(r.stdout+r.stderr)
        return r.stdout.strip()
    check=h.check
    admin,client,beta,gamma,trainer,anonymous=[h.Session() for _ in range(6)]
    admin.post('/Account/Login',dict(Email='admin.adminreports.test@healthtrack.example',Password=h.PASSWORD))
    trainer.post('/Account/Login',dict(Email='trainer@healthtrack.com',Password='Trainer@12345'))
    def binary(path):
        with admin.opener.open(h.BASE+path,timeout=45) as r:return r.headers,r.read()
    def exports():
        headers,data=binary('/Admin/ExportReport?format=csv')
        rows=list(csv.reader(io.StringIO(data.decode('utf-8-sig'))))
        headers,pdf=binary('/Admin/ExportReport?format=pdf')
        text=' '.join(' '.join(p.extract_text().split()) for p in PdfReader(io.BytesIO(pdf)).pages)
        return rows,text
    page=admin.request('/Admin/Reports')
    check('Reports with no Clients or plans',page[0]==200 and 'No assigned workout routines yet' in page[2] and 'No clients have registered yet' in page[2])
    rows,pdf=exports()
    check('Empty CSV export includes assigned routines and zero coverage',['Total assigned workout plans','0'] in rows and ['Active plan coverage (%)','0'] in rows)
    check('Empty PDF export valid with report sections','Assigned Workout Plan Summary' in pdf and 'Assigned Workout Routines' in pdf and 'Personal Workout Activities' in pdf)
    for role,s in [('Anonymous',anonymous),('Trainer',trainer)]:
        for path in ['/Admin/Reports','/Admin/ExportReport?format=csv','/Admin/ExportReport?format=pdf']:
            check(role+' blocked '+path,s.request(path)[0]==302)
    for s,email in [(client,'alpha.adminreports@healthtrack.example'),(beta,'beta.adminreports@healthtrack.example'),(gamma,'gamma.adminreports@healthtrack.example')]:h.register(s,email)
    for path in ['/Admin/Reports','/Admin/ExportReport?format=csv','/Admin/ExportReport?format=pdf']:
        check('Client blocked '+path,client.request(path)[0]==302)
    sql("""
    UPDATE Users SET Name='Alpha QA' WHERE Email='alpha.adminreports@healthtrack.example';
    UPDATE Users SET Name='Beta QA' WHERE Email='beta.adminreports@healthtrack.example';
    UPDATE Users SET Name='Gamma QA' WHERE Email='gamma.adminreports@healthtrack.example';
    DECLARE @a nvarchar(450)=(SELECT Id FROM Users WHERE Email='alpha.adminreports@healthtrack.example');
    DECLARE @b nvarchar(450)=(SELECT Id FROM Users WHERE Email='beta.adminreports@healthtrack.example');
    DECLARE @t nvarchar(450)=(SELECT Id FROM Users WHERE Email='trainer@healthtrack.com');
    DECLARE @today date=CAST(DATEADD(minute,330,SYSUTCDATETIME()) AS date);
    INSERT ClientTrainerAssignments (ClientId,TrainerId,AssignedAtUtc) VALUES (@a,@t,SYSUTCDATETIME());
    INSERT WorkoutPlans (TrainerId,ClientId,Title,StartDate,EndDate,IsActive,CreatedAtUtc) VALUES
    (@t,@a,'Shared routine',@today,@today,1,SYSUTCDATETIME()),
    (@t,@a,'Shared routine',@today,NULL,1,SYSUTCDATETIME()),
    (@t,@a,'Ended routine',DATEADD(day,-10,@today),DATEADD(day,-1,@today),1,SYSUTCDATETIME()),
    (@t,@b,'Future routine',DATEADD(day,1,@today),NULL,1,SYSUTCDATETIME()),
    (@t,@b,'Inactive routine',@today,NULL,0,SYSUTCDATETIME()),
    (@t,@b,'Preserved routine',@today,NULL,1,SYSUTCDATETIME());
    INSERT WorkoutPlanItems (WorkoutPlanId,ExerciseName,Sets,Reps) VALUES (1,'Squats',3,12),(1,'Pushups',3,10),(2,'Walking',NULL,NULL);
    """)
    for activity,minutes,calories in [('Shared routine',20,80),('Free text walking',30,120)]:
        assert client.post('/WorkoutLogs/Create',dict(Activity=activity,DurationMinutes=minutes,CaloriesBurned=calories,LoggedAt='2026-01-15T12:00'))[0]==302
    assert client.post('/MealLogs/Create',dict(MealType=0,FoodItems='Oats',Calories=300,LoggedAt='2026-01-15T12:00'))[0]==302
    page=admin.request('/Admin/Reports')[2]
    def metric(key):
        m=re.search(r'data-report-metric="'+key+r'">([^<]+)',page)
        return m.group(1).strip() if m else None
    for label,key,value in [('Total plans','total-plans','6'),('Active plans','active-plans','3'),('Distinct active Clients','active-clients','2'),('Clients with any plans','any-clients','2'),('Clients without plans','no-plan-clients','1'),('Current assigned Clients','assigned-clients','1'),('Current unassigned Clients','unassigned-clients','2')]:
        check(label+' matches fixture',metric(key)==value)
    check('Coverage uses all Clients as denominator',metric('coverage')=='66.7% of 3 Clients')
    check('Participation is clearly defined','not attendance or completion' in page)
    check('Same-titled plan records remain separate',page.count('<td>Shared routine</td>')==2)
    check('Statuses and original Trainer displayed',all(x in page for x in ['Active','Upcoming','Ended','Inactive','Coach Alex Rivera','Preserved routine']))
    check('Personal activity label corrected','Personal workout activities' in page and 'Popular routines' not in page)
    rows,pdf=exports()
    expected={'Total assigned workout plans':'6','Active assigned workout plans':'3','Clients with active assigned plans':'2','Active plan coverage (%)':'66.7','Clients with any assigned workout plans':'2','Clients without assigned workout plans':'1','Clients with a current Trainer assignment':'1','Clients without a current Trainer assignment':'2'}
    check('CSV assigned summary matches HTML',all([k,v] in rows for k,v in expected.items()))
    section=rows.index(['Assigned Workout Routines']); plan_rows=[]
    for row in rows[section+2:]:
        if not row:break
        plan_rows.append(row)
    check('CSV exports all six distinct routine IDs',len(plan_rows)==6 and {r[0] for r in plan_rows}==set('123456'))
    check('CSV routine dates statuses and exercise counts',any(r[0]=='1' and r[-2:]==['Active','2'] for r in plan_rows) and any(r[0]=='4' and r[-2]=='Upcoming' for r in plan_rows))
    check('CSV preserves Client performance totals',any(len(r)==11 and r[0]=='Alpha QA' and r[6:10]==['2','50','200','1'] for r in rows))
    check('CSV preserves engagement calculations',['Total clients','3'] in rows and ['Clients with at least one workout','1'] in rows and ['Average workouts per client','0.7'] in rows)
    check('PDF summary values match CSV',all(re.sub(r'\s+', '', k+v) in re.sub(r'\s+', '', pdf) for k,v in expected.items()))
    check('PDF contains every assigned routine title and personal activity',all(x in pdf for x in ['Shared routine','Ended routine','Future routine','Inactive routine','Preserved routine','Personal Workout Activities','Free text walking']))
    check('Free-text logs do not change assigned-plan participation',metric('active-clients')=='2' and ['Active assigned workout plans','3'] in rows)
    sql("UPDATE WorkoutPlans SET IsActive=0 WHERE Id IN (1,2)")
    rows,pdf=exports()
    check('Saved plan changes update counts and both exports',['Active assigned workout plans','1'] in rows and ['Clients with active assigned plans','1'] in rows and 'Active assigned workout plans 1' in pdf and 'Clients with active assigned plans 1' in pdf)
    output=Path(__file__).resolve().parents[1]/'artifacts/adminreports/results.json'
    output.write_text(json.dumps(h.RESULTS,indent=2))
    return 0 if all(r['passed'] for r in h.RESULTS) else 1
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--database',required=True);p.add_argument('--server',required=True)
    raise SystemExit(main(p.parse_args()))


