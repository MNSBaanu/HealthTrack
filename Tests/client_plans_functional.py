"""B01 HTTP/SQL acceptance checks; run using Run-ClientPlanTests.ps1 only."""
import argparse
import json
import re
import subprocess
from pathlib import Path
import appointments_functional as http
http.BASE = 'http://localhost:57633'

def main(args):
    assert re.fullmatch(r'HealthTrack_ClientPlans_Test_[a-f0-9]{32}', args.database)
    def sql(query):
        result = subprocess.run(['sqlcmd', '-S', args.server, '-d', args.database,
            '-E', '-I', '-b', '-W', '-h', '-1', '-Q', 'SET NOCOUNT ON; '+query], capture_output=True, text=True)
        if result.returncode: raise RuntimeError(result.stdout+result.stderr)
        return result.stdout.strip()
    check = http.check
    alpha,beta,admin,trainer,anonymous = [http.Session() for _ in range(5)]
    http.register(alpha,'alpha.clientplans@healthtrack.example')
    http.register(beta,'beta.clientplans@healthtrack.example')
    admin.post('/Account/Login',dict(Email='admin.clientplans.test@healthtrack.example',Password=http.PASSWORD))
    trainer.post('/Account/Login',dict(Email='trainer@healthtrack.com',Password='Trainer@12345'))
    for path in ['/ClientPlans','/ClientPlans/Workout/1','/ClientPlans/Diet/1']:
        for label,session in [('Anonymous',anonymous),('Admin',admin),('Trainer',trainer)]:
            check(label+' cannot access '+path,session.request(path)[0]==302)
    page = alpha.request('/ClientPlans')[2]
    check('Both no-assignment empty states', 'No workout plans assigned yet' in page and 'No diet plans assigned yet' in page)
    check('Dashboard with no plans',alpha.request('/Client')[0]==200 and 'No active workout plans' in alpha.request('/Client')[2])
    sql("""
    DECLARE @a nvarchar(450)=(SELECT Id FROM Users WHERE Email='alpha.clientplans@healthtrack.example');
    DECLARE @b nvarchar(450)=(SELECT Id FROM Users WHERE Email='beta.clientplans@healthtrack.example');
    DECLARE @t nvarchar(450)=(SELECT Id FROM Users WHERE Email='trainer@healthtrack.com');
    DECLARE @today date=CAST(DATEADD(minute,330,SYSUTCDATETIME()) AS date);
    INSERT ClientTrainerAssignments (ClientId,TrainerId,AssignedAtUtc) VALUES (@a,@t,SYSUTCDATETIME());
    INSERT WorkoutPlans (TrainerId,ClientId,Title,Goal,Description,StartDate,EndDate,IsActive,CreatedAtUtc) VALUES
    (@t,@a,'Alpha strength','Build strength','<script>planUnsafe</script>',@today,@today,1,SYSUTCDATETIME()),
    (@t,@b,'Beta private workout',NULL,NULL,@today,NULL,1,SYSUTCDATETIME()),
    (@t,@a,'Future workout',NULL,NULL,DATEADD(day,1,@today),NULL,1,SYSUTCDATETIME()),
    (@t,@a,'Ended workout',NULL,NULL,DATEADD(day,-10,@today),DATEADD(day,-1,@today),1,SYSUTCDATETIME()),
    (@t,@a,'Inactive workout',NULL,NULL,@today,NULL,0,SYSUTCDATETIME()),
    (@t,@a,'Empty workout',NULL,NULL,@today,NULL,1,SYSUTCDATETIME());
    INSERT WorkoutPlanItems (WorkoutPlanId,DayOfWeek,ExerciseName,Sets,Reps,DurationMinutes,RestSeconds,Instructions) VALUES
    (1,1,'Squats',3,12,20,60,'Controlled movement'),(1,NULL,'Stretch',NULL,NULL,5,NULL,'<img src=x onerror=alert(1)>');
    INSERT DietPlans (TrainerId,ClientId,Title,DailyCalorieTarget,TargetProteinGrams,TargetCarbsGrams,TargetFatGrams,Description,StartDate,EndDate,IsActive,CreatedAtUtc) VALUES
    (@t,@a,'Alpha daily diet',2200,150,220,65,'Daily nutrition',@today,NULL,1,SYSUTCDATETIME()),
    (@t,@b,'Beta private diet',2000,NULL,NULL,NULL,NULL,@today,NULL,1,SYSUTCDATETIME()),
    (@t,@a,'Future diet',2000,NULL,NULL,NULL,NULL,DATEADD(day,1,@today),NULL,1,SYSUTCDATETIME()),
    (@t,@a,'Ended diet',2000,NULL,NULL,NULL,NULL,DATEADD(day,-10,@today),DATEADD(day,-1,@today),1,SYSUTCDATETIME()),
    (@t,@a,'Inactive diet',2000,NULL,NULL,NULL,NULL,@today,NULL,0,SYSUTCDATETIME()),
    (@t,@a,'Empty diet',2000,NULL,NULL,NULL,NULL,@today,NULL,1,SYSUTCDATETIME());
    INSERT DietPlanMeals (DietPlanId,MealType,FoodRecommendations,ApproximateCalories,Notes) VALUES
    (1,'Breakfast','Oats and banana',500,'Drink water'),(1,'Dinner','<script>foodUnsafe</script>',650,NULL);
    """)
    listing=alpha.request('/ClientPlans')[2]
    check('List only includes own plans','Alpha strength' in listing and 'Alpha daily diet' in listing and 'Beta private' not in listing)
    check('List statuses include active upcoming ended inactive',all(x in listing for x in ['Active','Upcoming','Ended','Inactive']))
    check('Plans navigation visible','href="/ClientPlans"' in listing)
    workout=alpha.request('/ClientPlans/Workout/1')[2]
    diet=alpha.request('/ClientPlans/Diet/1')[2]
    check('Workout details and weekly schedule',all(x in workout for x in ['Squats','Monday','Sets: 3','Reps: 12','20 min','60 sec','Controlled movement','Day not specified','Build strength','Coach Alex Rivera','Active dates']))
    check('Diet details and daily targets',all(x in diet for x in ['Breakfast','Dinner','Oats and banana','500 kcal','150 g','220 g','65 g','Drink water','No end date','Coach Alex Rivera']))
    check('Diet does not invent unavailable meal times','Exact meal times are not specified' in diet)
    check('Trainer content HTML escaped','<script>planUnsafe' not in workout and '&lt;script&gt;planUnsafe' in workout and '<img src=x' not in workout and '<script>foodUnsafe' not in diet and '&lt;script&gt;foodUnsafe' in diet)
    for kind in ['Workout','Diet']:
        check(kind+' rejects another Client ID',beta.request('/ClientPlans/'+kind+'/1')[0]==404 and alpha.request('/ClientPlans/'+kind+'/2')[0]==404)
        check(kind+' rejects nonexistent ID',alpha.request('/ClientPlans/'+kind+'/999999')[0]==404)
        check(kind+' ignores forged ClientId query',beta.request('/ClientPlans/'+kind+'/1?clientId=alpha')[0]==404)
        check(kind+' has child-item empty state','No '+('exercises' if kind=='Workout' else 'meal items')+' have been added' in alpha.request('/ClientPlans/'+kind+'/6')[2])
        check(kind+' cannot be modified through Client endpoint',alpha.request('/ClientPlans/'+kind+'/1',dict(Title='Tampered'))[0] in (400,404,405))
    dashboard=alpha.request('/Client')[2]
    check('Dashboard links to both current plans','href="/ClientPlans/Workout/1"' in dashboard and 'href="/ClientPlans/Diet/1"' in dashboard)
    check('Dashboard excludes future ended inactive and other Client plans',all(x not in dashboard for x in ['Future workout','Future diet','Ended workout','Ended diet','Inactive workout','Inactive diet','Beta private']))
    sql("UPDATE WorkoutPlans SET Title='Updated strength' WHERE Id=1; UPDATE WorkoutPlanItems SET Reps=15 WHERE WorkoutPlanId=1 AND ExerciseName='Squats'; UPDATE DietPlanMeals SET FoodRecommendations='Updated breakfast' WHERE DietPlanId=1 AND MealType='Breakfast'; UPDATE DietPlans SET DailyCalorieTarget=2300 WHERE Id=1;")
    check('Saved workout changes reflected','Updated strength' in alpha.request('/Client')[2] and 'Reps: 15' in alpha.request('/ClientPlans/Workout/1')[2])
    check('Saved diet changes reflected','2,300 kcal' in alpha.request('/Client')[2] and 'Updated breakfast' in alpha.request('/ClientPlans/Diet/1')[2])
    sql("DELETE ClientTrainerAssignments WHERE ClientId=(SELECT Id FROM Users WHERE Email='alpha.clientplans@healthtrack.example')")
    check('Removing assignment preserves original plans and Trainer','Coach Alex Rivera' in alpha.request('/ClientPlans/Workout/1')[2] and 'Alpha daily diet' in alpha.request('/ClientPlans')[2])
    logged='2026-01-15T12:00'
    check('Workout logging still persists',alpha.post('/WorkoutLogs/Create',dict(Activity='B01 QA walk',DurationMinutes=20,CaloriesBurned=90,LoggedAt=logged))[0]==302 and sql("SELECT COUNT(*) FROM WorkoutLogs WHERE Activity='B01 QA walk'")=='1')
    check('Meal logging still persists',alpha.post('/MealLogs/Create',dict(MealType=0,FoodItems='B01 QA oats',Calories=300,LoggedAt=logged))[0]==302 and sql("SELECT COUNT(*) FROM MealLogs WHERE FoodItems='B01 QA oats'")=='1')
    check('Personal journals separate from assigned plans','Updated strength' not in alpha.request('/WorkoutLogs')[2] and 'Alpha daily diet' not in alpha.request('/MealLogs')[2])
    for path in ['/WorkoutLogs','/MealLogs','/HealthMetrics','/Client/Progress','/Appointments']:
        check('Existing Client page works '+path,alpha.request(path)[0]==200)
    output=Path(__file__).resolve().parents[1]/'artifacts/clientplans/results.json'
    output.write_text(json.dumps(http.RESULTS,indent=2))
    return 0 if all(r['passed'] for r in http.RESULTS) else 1

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--database',required=True)
    parser.add_argument('--server',required=True)
    raise SystemExit(main(parser.parse_args()))
