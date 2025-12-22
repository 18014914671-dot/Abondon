-- update: 26/10/2020 17:00
function gtCd(x,y,r,an)
  cd={}
  cd.x=x+math.sin(an)*r
  cd.y=y+math.cos(an)*r
  return cd
end
function drRadar(wi,he,r,col,time)
  delay(time)
  x=wi/2
  y=he/2
  alpha(0.5)
  for i=1,360,1 do
    an=math.rad(i)
    c=gtCd(x,y,r,an)
    point(c.x,c.y,col)
    c=gtCd(x,y,r+10,an)
    point(c.x,c.y,col)
    c=gtCd(x,y,r+20,an)
    point(c.x,c.y,col)
    c=gtCd(x,y,r+30,an)
    point(c.x,c.y,col)
    c=gtCd(x,y,r+40,an)
    point(c.x,c.y,col)
    c=gtCd(x,y,r+50,an)
    point(c.x,c.y,col)
    c=gtCd(x,y,r+60,an)
    point(c.x,c.y,col)
    c=gtCd(x,y,r+70,an)
    point(c.x,c.y,col)
    render()
  end
end
function drRay(wi,he,r,col,time)
  delay(time)
  x=wi/2
  y=he/2
  gx=40
  gy=40
  alpha(0.3)
  for i=1,720,1 do
    an=math.rad(i)
    c=gtCd(x,y,r,an)
    line(x,y,c.x,c.y,col)
    if i>15 then
      an=math.rad(i-15)
      d=gtCd(x,y,r,an)
      line(x,y,d.x,d.y,0)
      alpha(1)
      drTarget(col)
      drFrame(col)
      drGC(gx,gy)
      alpha(0.5)
      if i==20 or i==100 or i==180 or i==300 then 
        gx=gx+20
        gy=gy+2
      end
      if i>=160 then
        col=8
        text("UFO GAMECODEUR DETECT",20,100,8)
      end
    end
    render()
  end
end
function wait(time)
  delay(time)
  render()
end
function drTarget(col)
  line(80,0,80,10,col)
  line(80,150,80,160,col)
  line(0,80,10,80,col)
  line(150,80,160,80,col)
  line(80,77,80,83,col)
  line(77,80,83,80,col)
end
function drFrame(col)
  line(0,0,160,0,col)
  line(0,159,160,159,col)
  line(0,1,0,160,col)
  line(159,0,159,160,col)
end
function drTxt(txt,x,y,col,t)
  text(txt,x,y,col)
  wait(t)
  text(txt,x,y,0)
end
function drGC(x,y)
  textb ("GC",x,y,3,11)
end
function draw()
  clear()
  ray=10
  wi=160
  he=160
  col=11
  drFrame(col)
  drTarget(col)
  drTxt("Start GC radar",40,40,col,1500)
  drRadar(wi,he,ray,col,16)
  drRay(wi,he,ray+70,col,16)
end
