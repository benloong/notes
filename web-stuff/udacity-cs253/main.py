#!/usr/bin/env python
#
# Copyright 2007 Google Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
import webapp2
import cgi
import jinja2
import os

from google.appengine.ext import db

template_dir = os.path.join(os.path.dirname(__file__), "templates")
jinja_env = jinja2.Environment(loader = jinja2.FileSystemLoader(template_dir), autoescape = True)
def escape_html(s):
    return cgi.escape(s, quote = True)

form = """
<form method="post">
    What is your birthday?
    <br>
    <label>
        Month
        <input type="text" name="month" value="%(month)s">
    </label>
    <label>
        Day
        <input type="text" name="day" value="%(day)s">
    </label>
    <label>
        Year
        <input type="text" name="year" value="%(year)s">
    </label>
    <div style="color: red">%(error)s</div>
    <br><br>
    <input type="submit">
</form>
"""

months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August',
                        'September', 'October', 'November', 'December']

def valid_day(day):
        if(day and day.isdigit()):
                day = int(day)
        if(day < 32 and day > 0):
                return day

def valid_month(month):
        if(month):
                month = month.capitalize()
        if(month in months):
                return month

def valid_year(year):
        if(year and year.isdigit()):
                year = int(year)
        if(year < 2020 and year > 1880):
                return year

SECRET = "023sd12lfkks"

import hmac
def hash_str(s):
	return hmac.new(SECRET,s).hexdigest()

def make_secure_val(s):
	return "%s|%s" % (s, hash_str(s))

def check_val(h):
	val = h.split("|")[0]
	if h == make_secure_val(val):
		return val


import string
import random

def make_salt():
	return ''.join(random.choice(string.hexdigits) for x in xrange(5))

def make_pw_hash(name, pw, salt=None):
    if not salt:
        salt=make_salt()
    h = hashlib.sha256(name + pw + salt).hexdigest()
    return '%s%s' % (h, salt)

def valid_pw(name, pw, h):
    salt = h.split(',')[1]
    return h == make_pw_hash(name, pw, salt)


class Art(db.Model):
	title = db.StringProperty(required = True)
	art = db.TextProperty(required = True)
	created = db.DateTimeProperty(auto_now_add = True)

class MainPage(webapp2.RequestHandler):
    def write_form(self, error="", month="", day="", year=""):
        self.response.out.write(form %{"error": error,
                                       "month": escape_html(month),
                                       "day": escape_html(day),
                                       "year": escape_html(year)})

    def get(self):
        self.write_form()

    def post(self):
        user_month = self.request.get('month')
        user_day = self.request.get('day')
        user_year = self.request.get('year')

        month = valid_month(user_month)
        day = valid_day(user_day)
        year = valid_year(user_year)

        if not(month and day and year):
            self.write_form("That doesn't look valid to me, friend.", user_month, user_day, user_year)
        else:
            self.redirect("/thanks")

class ThanksHandler(webapp2.RequestHandler):
    def get(self):
        self.response.out.write("Thanks! That's a totally valid day!")

class AsciiChanHandler(webapp2.RequestHandler):
    def write(self, *a, **kw):
        self.response.out.write(*a, **kw)
    def render_str(self, template, **params):
        t = jinja_env.get_template(template)
        return t.render(params)
    def render(self, template, **kw):
        self.write(self.render_str(template, **kw))

    def render_front(self, title="", art="", error=""):
    	arts = db.GqlQuery("SELECT * FROM Art ORDER BY created DESC")
    	self.render("asciiart.html", title=title, art=art, error = error, arts = arts)

    def get(self):
    	self.render_front()
    	
    def post(self):
    	title = self.request.get("title")
    	art = self.request.get("art")
    	if title and art:
    		a = Art(title = title, art = art)
    		a.put()
    		self.redirect("/ascii")
    	else:
    		error = "we need both a title and some art"
    		self.render_front(error=error)

class CookieHandler(webapp2.RequestHandler):
	"""Cookie stuff"""
	def get(self):
		self.response.headers["Content-Type"] = "text/plain"
		visits = check_val(self.request.cookies.get("visits", 0))
		
		if visits and visits.isdigit():
			visits = int(visits) + 1
		else:
			visits = 0

		self.response.headers.add_header("Set-Cookie", "visits=%s" % make_secure_val(str(visits)))
		self.write("visits count: %s" % visits)
	def write(self, *a, **kw):	
		self.response.out.write(*a, **kw);
	def render_str(self, template, **params):
		t = jinja_env.get_template(template)
		return t.render(params)
	def render(self, template, **kw):
		self.write(self.render_str(template, **kw))
		
app = webapp2.WSGIApplication([('/', CookieHandler),
                              ('/thanks', ThanksHandler),
                              ('/ascii', AsciiChanHandler),
                              ('/cookie', CookieHandler)
                              ],
                             debug=True)