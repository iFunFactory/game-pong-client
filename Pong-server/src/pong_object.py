#!/usr/bin/env python
# -*- coding: utf-8 -*-
# Copyright (C) 2013-2015 iFunFactory Inc. All Rights Reserved.
#
# This work is confidential and proprietary to iFunFactory Inc. and
# must not be used, disclosed, copied, or distributed without the prior
# consent of iFunFactory Inc.


# Example.
#
# AppId/ProjectName
#   MyGame
#
# Object Model
#
#   {
#     "Character": {
#       "Name": "String KEY",
#       "Level": "Integer",
#       "Hp": "Integer",
#       "Mp": "Integer"
#     }
#   }
#
# Create/Fetch/Delete Example
#
#   import my_game_object
#
#   def Example():
#     # Initialize
#     # my_game_object.initialize(mysql id, mysql pw, mysql server address, database name, zookeeper hosts, app id)
#     my_game_object.initialize('db_id', 'db_pw', '127.0.0.1', 'my_game_db', '127.0.0.1', 'MyGame')
#
#     # Create an object
#     character = my_game_object.Character.create('mycharacter')
#     character.set_Level(1)
#     character.set_Hp(150)
#     character.set_Mp(70)
#     character.commit()
#
#     # Fetch an object
#     character = my_game_object.Character.fetch_by_Name('mycharacter2')
#     character.set_Level(character.get_Level() + 1)
#     print character.get_Name()
#     print character.get_Level()
#     print character.get_Hp()
#     print character.get_Mp()
#     character.commit()
#
#     # Delete an object
#     character = my_game_object.Character.fetch_by_Name('mycharacter3')
#     character.delete()
#     character.commit()


import binascii
import sys
sys.path.append('/usr/share/funapi/python')

import funapi.object.object as funapi


def initialize(user, password, host, database, zookeeper_hosts, app_name):
  funapi.MysqlConnection.initialize(user, password, host, database)
  funapi.Zookeeper.initialize(zookeeper_hosts, app_name)


object = funapi.ObjectModel('Character')
attribute = funapi.AttributeModel('Name', 'String', True, False, False, False, False, '')
object.add_attribute_model(attribute)
attribute = funapi.AttributeModel('Exp', 'Integer', False, False, False, False, False, '')
object.add_attribute_model(attribute)
attribute = funapi.AttributeModel('Level', 'Integer', False, False, False, False, False, '')
object.add_attribute_model(attribute)
attribute = funapi.AttributeModel('Hp', 'Integer', False, False, False, False, False, '')
object.add_attribute_model(attribute)
attribute = funapi.AttributeModel('Mp', 'Integer', False, False, False, False, False, '')
object.add_attribute_model(attribute)
attribute = funapi.AttributeModel('_tag', 'String', False, False, False, False, False, '')
object.add_attribute_model(attribute)
funapi.ObjectModel.add_object_model(object)
del attribute
del object


object = funapi.ObjectModel('User')
attribute = funapi.AttributeModel('Id', 'String', True, False, False, False, False, '')
object.add_attribute_model(attribute)
attribute = funapi.AttributeModel('MyCharacter', 'Character', False, False, False, False, True, '')
object.add_attribute_model(attribute)
attribute = funapi.AttributeModel('tmp', 'Integer', False, False, False, False, False, '')
object.add_attribute_model(attribute)
attribute = funapi.AttributeModel('_tag', 'String', False, False, False, False, False, '')
object.add_attribute_model(attribute)
funapi.ObjectModel.add_object_model(object)
del attribute
del object


class Character:
  @staticmethod
  def create(Name):
    model = funapi.ObjectModel.get_object_model('Character')
    attributes = {}
    attributes['Name'] = Name
    obj = funapi.Object.create(model, attributes)
    return Character(obj)

  @staticmethod
  def fetch(object_id):
    if len(object_id) == 16:
      object_id = binascii.hexlify(object_id)
    model = funapi.ObjectModel.get_object_model('Character')
    obj = funapi.Object.fetch(model, object_id)
    if obj == None:
      return None
    return Character(obj)

  @staticmethod
  def fetch_by_Name(value):
    model = funapi.ObjectModel.get_object_model('Character')
    obj = funapi.Object.fetch_by(model, 'Name', value)
    if obj == None:
      return None
    return Character(obj)

  def __init__(self, obj):
    self.object_ = obj

  def commit(self):
    self.object_.commit()

  def delete(self):
    self.object_.delete()

  def get_object_id(self):
    return self.object_.get_object_id()

  def get_Name(self):
    return self.object_.get_attribute('Name')

  def set_Name(self, value):
    self.object_.set_attribute('Name', value)

  def get_Exp(self):
    return self.object_.get_attribute('Exp')

  def set_Exp(self, value):
    self.object_.set_attribute('Exp', value)

  def get_Level(self):
    return self.object_.get_attribute('Level')

  def set_Level(self, value):
    self.object_.set_attribute('Level', value)

  def get_Hp(self):
    return self.object_.get_attribute('Hp')

  def set_Hp(self, value):
    self.object_.set_attribute('Hp', value)

  def get_Mp(self):
    return self.object_.get_attribute('Mp')

  def set_Mp(self, value):
    self.object_.set_attribute('Mp', value)

  def get__tag(self):
    return self.object_.get_attribute('_tag')

  def set__tag(self, value):
    self.object_.set_attribute('_tag', value)


class User:
  @staticmethod
  def create(Id):
    model = funapi.ObjectModel.get_object_model('User')
    attributes = {}
    attributes['Id'] = Id
    obj = funapi.Object.create(model, attributes)
    return User(obj)

  @staticmethod
  def fetch(object_id):
    if len(object_id) == 16:
      object_id = binascii.hexlify(object_id)
    model = funapi.ObjectModel.get_object_model('User')
    obj = funapi.Object.fetch(model, object_id)
    if obj == None:
      return None
    return User(obj)

  @staticmethod
  def fetch_by_Id(value):
    model = funapi.ObjectModel.get_object_model('User')
    obj = funapi.Object.fetch_by(model, 'Id', value)
    if obj == None:
      return None
    return User(obj)

  def __init__(self, obj):
    self.object_ = obj

  def commit(self):
    self.object_.commit()

  def delete(self):
    self.object_.delete()

  def get_object_id(self):
    return self.object_.get_object_id()

  def get_Id(self):
    return self.object_.get_attribute('Id')

  def set_Id(self, value):
    self.object_.set_attribute('Id', value)

  def get_MyCharacter(self):
    return self.object_.get_attribute('MyCharacter')

  def set_MyCharacter(self, value):
    self.object_.set_attribute('MyCharacter', value)

  def get_tmp(self):
    return self.object_.get_attribute('tmp')

  def set_tmp(self, value):
    self.object_.set_attribute('tmp', value)

  def get__tag(self):
    return self.object_.get_attribute('_tag')

  def set__tag(self, value):
    self.object_.set_attribute('_tag', value)

