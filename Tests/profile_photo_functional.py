"""Run using Run-ProfilePhotoTests.ps1 against a disposable database."""
import argparse
import base64
import io
import re
import urllib.error
import urllib.request
import appointments_functional as http
from PIL import Image

http.BASE = 'http://localhost:57629'
PNG = base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII=')

def request(session, path, body=None, content_type=None):
    headers = {'Content-Type': content_type} if content_type else {}
    try:
        response = session.opener.open(urllib.request.Request(http.BASE + path, data=body, headers=headers), timeout=45)
    except urllib.error.HTTPError as error:
        response = error
    return response.code, response.headers, response.read()

def upload(session, data, filename='photo.png', mime='image/png', token=True, valid_profile=True):
    boundary = 'HealthTrackPhotoTestBoundary'
    fields = dict(Name='Photo QA', DateOfBirth='1995-01-15', HeightCm='175', FitnessGoal='Fitness')
    if not valid_profile: fields['HeightCm'] = '1'
    if token: fields['__RequestVerificationToken'] = session.token('/Client/Profile')
    parts = []
    for key, value in fields.items():
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode())
    parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="Photo"; filename="{filename}"\r\nContent-Type: {mime}\r\n\r\n'.encode() + data + b'\r\n')
    parts.append(f'--{boundary}--\r\n'.encode())
    return request(session, '/Client/Profile', b''.join(parts), f'multipart/form-data; boundary={boundary}')

def main():
    first, second, anonymous, admin = http.Session(), http.Session(), http.Session(), http.Session()
    http.register(first, 'photo.first@healthtrack.example')
    http.register(second, 'photo.second@healthtrack.example')
    admin.post('/Account/Login', dict(Email='admin.profilephoto.test@healthtrack.example', Password=http.PASSWORD))
    check = http.check
    check('Clients without photos retain initials', 'id="profile-photo-initials"' in first.request('/Client/Profile')[2])
    check('No photo returns 404', request(first, '/Client/ProfilePhoto')[0] == 404)
    check('Anonymous photo access requires login', request(anonymous, '/Client/ProfilePhoto')[0] == 302)
    check('Admin cannot use Client photo endpoint', request(admin, '/Client/ProfilePhoto')[0] == 302)
    check('Upload requires antiforgery token', upload(first, PNG, token=False)[0] == 400)
    result = upload(first, PNG)
    check('PNG photo saves successfully', result[0] == 302)
    photo = request(first, '/Client/ProfilePhoto')
    check('Saved photo persists with correct bytes and MIME type', photo[0] == 200 and photo[2] == PNG and photo[1].get('Content-Type') == 'image/png')
    check('Photo response prevents caching and MIME sniffing', 'no-store' in photo[1].get('Cache-Control', '') and photo[1].get('X-Content-Type-Options') == 'nosniff')
    check('Other Client cannot retrieve uploaded photo through an ID parameter', request(second, '/Client/ProfilePhoto?id=photo.first@healthtrack.example')[0] == 404)
    check('Profile renders saved photo', 'src="/Client/ProfilePhoto"' in first.request('/Client/Profile')[2])
    for name, data, filename, mime in [
        ('SVG rejected', b'<svg xmlns="http://www.w3.org/2000/svg"></svg>', 'photo.svg', 'image/svg+xml'),
        ('HTML disguised as PNG rejected', b'<html>bad file</html>', 'photo.png', 'image/png'),
        ('Empty file rejected', b'', 'photo.png', 'image/png'),
        ('Photo over 2 MB rejected', PNG + b'x' * (2 * 1024 * 1024), 'photo.png', 'image/png'),
        ('Truncated PNG rejected', PNG[:32], 'photo.png', 'image/png'),
        ('Unsupported extension rejected', PNG, 'photo.txt', 'image/png'),
        ('Oversized image dimensions rejected', PNG[:16] + (5000).to_bytes(4, 'big') + PNG[20:], 'photo.png', 'image/png')
    ]:
        result = upload(first, data, filename, mime)
        check(name, result[0] == 200 and b'field-validation-error' in result[2])
    check('Rejected uploads preserve saved photo', request(first, '/Client/ProfilePhoto')[2] == PNG)
    check('Invalid profile details prevent photo update', upload(first, PNG, valid_profile=False)[0] == 200)
    saved = first.post('/Client/Profile', dict(Name='Updated Photo QA', DateOfBirth='1995-01-15', HeightCm=175))
    check('Saving profile without a file preserves photo', saved[0] == 302 and request(first, '/Client/ProfilePhoto')[2] == PNG)
    check('Submitted MIME type cannot control stored photo type', upload(first, PNG, mime='text/html')[0] == 302 and request(first, '/Client/ProfilePhoto')[1].get('Content-Type') == 'image/png')
    jpeg_buffer = io.BytesIO()
    Image.new('RGB', (2, 2), 'yellow').save(jpeg_buffer, format='JPEG')
    jpeg = jpeg_buffer.getvalue()
    check('JPEG can replace an existing PNG photo', upload(first, jpeg, 'photo.jpg', 'image/jpeg')[0] == 302 and request(first, '/Client/ProfilePhoto')[2] == jpeg)
    check('JPEG response uses image/jpeg', request(first, '/Client/ProfilePhoto')[1].get('Content-Type') == 'image/jpeg')
    check('Each Client retrieves only their own stored photo', upload(second, PNG)[0] == 302 and request(second, '/Client/ProfilePhoto')[2] == PNG and request(first, '/Client/ProfilePhoto')[2] == jpeg)
    return 0 if all(item['passed'] for item in http.RESULTS) else 1

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--database', required=True)
    parser.add_argument('--server', required=True)
    args = parser.parse_args()
    assert re.fullmatch(r'HealthTrack_ProfilePhoto_Test_[a-f0-9]{32}', args.database)
    raise SystemExit(main())
