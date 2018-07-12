using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BridgeServer_WebClient.Models;
using BridgeServer_WebClient.Helper;
using log4net;
namespace BridgeServer_WebClient.Controllers
{
    public class MaterialController : Controller
    {
        private ServerContext db = new ServerContext();
        private ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: DeviceProperties
        public ActionResult Index()
        {
            List<Material> materials = (from d in db.Materials
                                        where d.enabled == true
                                        orderby d.Identifier
                                        select d).ToList();

            if (!ServerInfoHelper.isRunning())
            {
                foreach (Material material in materials)
                {
                    foreach (Device device in material.Devices)
                    {
                        device.Online = false;
                    }
                }
            }


            return View(materials);
        }

        // GET: DeviceProperties/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Material material = db.Materials.Find(id);
            if (material == null)
            {
                return HttpNotFound();
            }
            return View(material);
        }

        // GET: DeviceProperties/Create
        public ActionResult Create()
        {
            CreateMaterialViewModel vm = new CreateMaterialViewModel();

            List<Device> devices = (from d in db.Devices
                                    where d.Materials.Count == 0
                                    orderby d.Identifier
                                    select d).ToList();

            devices.Insert(0, new Device());
            vm.DeviceList = from device in devices
                            select new SelectListItem
                            {
                                Text = device.Identifier,
                                Value = device.ID.ToString()
                            };

            return View(vm);
        }

        // POST: DeviceProperties/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateMaterialViewModel vm)
        {
            if (ModelState.IsValid)
            {
                Material dbMaterial = (from d in db.Materials
                                       where d.Identifier == vm.Material.Identifier
                                       select d).FirstOrDefault();
                if (dbMaterial == null)
                {

                    vm.Material.ID = Guid.NewGuid();
                    Device device = null;
                    if (vm.DeviceID != null && !vm.DeviceID.Equals(""))
                    {
                        Guid deviceId = Guid.Parse(vm.DeviceID);
                        if (!deviceId.Equals(Guid.Empty))
                        {
                            device = (from a in db.Devices
                                      where a.ID == deviceId
                                      select a).First();
                            vm.Material.Devices.Add(device);
                        }

                    }
                    vm.Material.enabled = true;
                    db.Materials.Add(vm.Material);
                    db.SaveChanges();
                    if (device != null)
                    {
                        logger.Info(device.Identifier + " Pair " + vm.Material.Identifier + " device ID= " + device.ID + " Material ID= " + vm.Material.ID);
                    }
                    else
                    {
                        logger.Info("NO Pair " + vm.Material.Identifier + " device ID= null" + " Material ID= " + vm.Material.ID);
                    }
                    return RedirectToAction("Index");
                }
                else
                {
                    List<Device> devices = (from d in db.Devices
                                            where d.Materials.Count == 0
                                            select d).ToList();

                    devices.Insert(0, new Device());
                    vm.DeviceList = from device in devices
                                    select new SelectListItem
                                    {
                                        Text = device.Identifier,
                                        Value = device.ID.ToString()
                                    };
                    ModelState.AddModelError("Material.Identifier", string.Format("{0} is already exit.", vm.Material.Identifier));
                }
            }

            return View(vm);
        }

        // GET: DeviceProperties/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Material material = db.Materials.Find(id);
            if (material == null)
            {
                return HttpNotFound();
            }

            CreateMaterialViewModel vm = new CreateMaterialViewModel();
            vm.Material = material;
            List<Device> devices = (from d in db.Devices
                                    orderby d.Identifier
                                    select d).ToList();

            var dd = from d in devices
                     where (d.Materials.Count == 0 || (d.Materials.Count > 0 && d.Materials.First().ID == material.ID))
                     select d;
            if (material.Devices.Count > 0)
            {
                Device pairDevice = material.Devices.First();
                vm.DeviceList = from device in dd
                                select new SelectListItem
                                {
                                    Selected = (device.ID == pairDevice.ID) ? true : false,
                                    Text = device.Identifier,
                                    Value = device.ID.ToString()
                                };
            }
            else
            {
                vm.DeviceList = from device in dd
                                select new SelectListItem
                                {
                                    Text = device.Identifier,
                                    Value = device.ID.ToString()
                                };
            }


            return View(vm);
        }

        // POST: DeviceProperties/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Edit(CreateMaterialViewModel vm, string IdentifierOld, string deviceID, string deviceIdentifier)
        {
            if (ModelState.IsValid)
            {
                vm.Material.enabled = true;
                db.Entry(vm.Material).State = EntityState.Modified;
                db.SaveChanges();
                logger.Info("deviceIdentifier= " + deviceIdentifier + " " + IdentifierOld + " Modify to " + vm.Material.Identifier.ToString() + " deviceID= " + deviceID);
                return RedirectToAction("Index");
            }
            return View(vm);
        }


        public ActionResult Pair(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Material material = db.Materials.Find(id);
            if (material == null)
            {
                return HttpNotFound();
            }

            CreateMaterialViewModel vm = new CreateMaterialViewModel();
            vm.Material = material;
            IEnumerable<Device> devices = from d in db.Devices
                                          where d.Materials.Count == 0
                                          select d;

            vm.DeviceList = from device in devices
                            orderby device.Identifier
                            select new SelectListItem
                            {
                                Text = device.Identifier,
                                Value = device.ID.ToString()
                            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pair(CreateMaterialViewModel vm)
        {
            if (ModelState.IsValid)
            {
                Guid deviceId = Guid.Parse(vm.DeviceID);

                Device device = (from a in db.Devices
                                 where a.ID == deviceId
                                 select a).First();

                Material material = db.Materials.Find(vm.Material.ID);
                material.Devices.Clear();
                material.Devices.Add(device);
                db.SaveChanges();
                logger.Info(material.Identifier + " Pair " + device.Identifier.ToString() + " materialID=" + vm.Material.ID + " deviceID=" + vm.DeviceID);
                return RedirectToAction("Index");
            }
            return View(vm);
        }


        // GET: DeviceProperties/Delete/5
        public ActionResult Unpair(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Material material = db.Materials.Find(id);
            if (material == null)
            {
                return HttpNotFound();
            }
            return View(material);
        }

        // POST: DeviceProperties/Delete/5
        [HttpPost, ActionName("Unpair")]
        [ValidateAntiForgeryToken]
        public ActionResult UnpairConfirmed(Guid id)
        {

            Material material = db.Materials.Find(id);
            
            Location location = (from l in db.Locations where l.Name.Equals("回溫區") select l).FirstOrDefault();
            try
            {
                if (material.Events.LastOrDefault()==null||material.Events.LastOrDefault().Location1.Equals(location.ID) == false)
                {
                    logger.Info(material.Identifier.ToString()+" Unpair 時不再回溫區");
                }
            }
            catch (Exception ex) { }
            logger.Info(material.Identifier.ToString() + " Unpair " + material.Devices.FirstOrDefault().Identifier.ToString() + " materialID= " + id + " deviceID=" + material.Devices.FirstOrDefault().ID.ToString());
            material.Devices.Clear();

            db.Entry(material).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: DeviceProperties/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Material material = db.Materials.Find(id);
            CreateMaterialViewModel vm = new CreateMaterialViewModel();
            vm.Material = material;
            List<Device> devices = (from d in db.Devices
                                    orderby d.Identifier
                                    select d).ToList();

            var dd = from d in devices
                     where (d.Materials.Count == 0 || (d.Materials.Count > 0 && d.Materials.First().ID == material.ID))
                     select d;
            if (material.Devices.Count > 0)
            {
                Device pairDevice = material.Devices.First();
                vm.DeviceList = from device in dd
                                select new SelectListItem
                                {
                                    Selected = (device.ID == pairDevice.ID) ? true : false,
                                    Text = device.Identifier,
                                    Value = device.ID.ToString()
                                };
            }
            else
            {
                vm.DeviceList = from device in dd
                                select new SelectListItem
                                {
                                    Text = device.Identifier,
                                    Value = device.ID.ToString()
                                };
            }

            return View(vm);
        }

        // POST: DeviceProperties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(CreateMaterialViewModel vm)
        {
            Guid materialUUID = vm.Material.ID;
            Material material = db.Materials.Find(materialUUID);
            logger.Info(material.Identifier);
            
            //material.Events.Clear();
            material.Devices.Clear();
            material.enabled = false;
            //db.Materials.Remove(material);
            db.SaveChanges();

            //db.Entry(material).Reload();
            //db.Materials.Remove(material);
            //db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
